using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;
using ACS_View.Infrastructure.Data.Migrations;

namespace ACS_View.UseCases.Services
{
    internal class VaccineService(IDatabaseService dbService, ICurrentUserContext currentUserContext) : IVaccineService
    {
        private readonly SQLiteAsyncConnection _database = dbService.Connection;

        public async Task<PatientVaccineScheduleDto?> GetScheduleForPatientAsync(int patientId)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            var patient = await _database.Table<Patient>()
                .FirstOrDefaultAsync(patient => patient.Id == patientId && patient.UserId == userId);

            if (patient is null)
            {
                return null;
            }

            var applicationRows = await _database.QueryAsync<PatientVaccineDose>(
                $"SELECT {LegacyDateSql.Projection<PatientVaccineDose>()} FROM PatientVaccineDose WHERE UserId=? AND PatientId=? AND IsApplied=1",
                userId, patientId);

            var applicationByDose = applicationRows
                .OrderBy(dose => dose.Id)
                .GroupBy(dose => dose.DoseKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var isPregnant = await IsPregnantAsync(patientId, userId);
            var ageMonths = GetMonth(patient.BirthDate);
            var doses = VaccineDoseCatalog
                .GetExpectedDefinitions(ageMonths, isPregnant)
                .Select(definition => BuildDoseDto(patient.BirthDate, definition, applicationByDose))
                .ToList();

            return new PatientVaccineScheduleDto
            {
                PatientId = patientId,
                BirthDate = patient.BirthDate,
                IsPregnant = isPregnant,
                ApplicationsByDose = doses
                    .Where(dose => dose.IsApplied)
                    .ToDictionary(dose => dose.Definition.DoseKey, StringComparer.OrdinalIgnoreCase),
                Doses = doses
            };
        }

        public async Task ApplyDoseAsync(VaccineApplicationRequestDto request)
        {
            var userId = currentUserContext.RequireCurrentUserId();

            if (VaccineDoseCatalog.GetDefinition(request.DoseKey) is null)
            {
                throw new ArgumentException("Dose de vacina invalida.", nameof(request));
            }

            if (request.ApplicationDate.HasValue && request.ApplicationDate.Value.Date > DateTime.Today)
            {
                throw new InvalidOperationException("A data de aplicacao nao pode ser futura.");
            }

            await _database.RunInTransactionAsync(connection =>
            {
                var patientExists = connection.Table<Patient>()
                    .Where(patient => patient.Id == request.PatientId && patient.UserId == userId)
                    .Count() > 0;
                if (!patientExists)
                {
                    throw new InvalidOperationException("Paciente nao encontrado.");
                }

                var existingRows = ReadDoseApplications(connection, userId, request.PatientId, request.DoseKey);
                if (existingRows.Count > 1)
                {
                    throw new InvalidOperationException("Há aplicações históricas distintas desta dose. Concilie os registros antes de editar.");
                }

                SaveApplication(connection, userId, request, existingRows.SingleOrDefault());
            });
            DataChangeTracker.MarkPatientsChanged();
        }

        public async Task ApplyOverdueDosesAsync(
            int patientId, int expectedUserId, IReadOnlyDictionary<string, DateTime> applicationDates)
        {
            var today = DateTime.Today;
            if (applicationDates.Count == 0 ||
                applicationDates.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).Count != applicationDates.Count)
            {
                throw new ArgumentException("Informe uma data para cada dose atrasada.", nameof(applicationDates));
            }

            await _database.RunInTransactionAsync(connection =>
            {
                if (!currentUserContext.HasCurrentUser || currentUserContext.CurrentUserId != expectedUserId)
                {
                    throw new InvalidOperationException("O perfil ativo mudou. Abra o cartão vacinal novamente.");
                }

                var patient = connection.Table<Patient>()
                    .FirstOrDefault(patient => patient.Id == patientId && patient.UserId == expectedUserId)
                    ?? throw new InvalidOperationException("Paciente não encontrado.");
                var conditions = connection.Table<PatientConditions>()
                    .Where(condition => condition.UserId == expectedUserId && condition.PatientId == patientId)
                    .ToList();
                var definitions = VaccineDoseCatalog.GetExpectedDefinitions(GetMonth(patient.BirthDate), conditions.Any(IsPregnancy))
                    .ToDictionary(definition => definition.DoseKey, StringComparer.OrdinalIgnoreCase);

                foreach (var (doseKey, date) in applicationDates)
                {
                    if (date.Date > today)
                    {
                        throw new InvalidOperationException("A data de aplicação não pode ser futura.");
                    }

                    var existingRows = ReadDoseApplications(connection, expectedUserId, patientId, doseKey);
                    if (!definitions.TryGetValue(doseKey, out var definition) ||
                        VaccineStatusCalculator.Calculate(patient.BirthDate, today, definition, null) != VaccineStatus.Late ||
                        existingRows.Count > 1 || existingRows.Any(row => row.IsApplied))
                    {
                        throw new InvalidOperationException("O cartão vacinal mudou ou contém registros conflitantes. Feche o lote e confira as doses.");
                    }

                    var existing = existingRows.SingleOrDefault();
                    var request = new VaccineApplicationRequestDto(
                        patientId, definition.DoseKey, date, existing?.LotNumber ?? string.Empty, existing?.Notes ?? string.Empty);
                    SaveApplication(connection, expectedUserId, request, existing);
                }
            });

            DataChangeTracker.MarkPatientsChanged();
        }

        private static List<PatientVaccineDose> ReadDoseApplications(SQLiteConnection connection, int userId, int patientId, string doseKey)
        {
            return connection.Query<PatientVaccineDose>(
                $"SELECT {LegacyDateSql.Projection<PatientVaccineDose>()} FROM PatientVaccineDose WHERE UserId=? AND PatientId=? AND DoseKey=? COLLATE NOCASE",
                userId, patientId, doseKey);
        }

        private static void SaveApplication(SQLiteConnection connection, int userId, VaccineApplicationRequestDto request, PatientVaccineDose? existing)
        {
            if (existing is null)
            {
                connection.Insert(new PatientVaccineDose
                {
                    UserId = userId,
                    PatientId = request.PatientId,
                    DoseKey = request.DoseKey,
                    IsApplied = true,
                    AppliedAt = request.ApplicationDate?.Date,
                    LotNumber = request.LotNumber.Trim(),
                    Notes = request.Notes.Trim(),
                    CreatedAt = DateTime.Now
                });
                return;
            }

            connection.Execute("UPDATE PatientVaccineDose SET IsApplied=1,AppliedAt=?,LotNumber=?,Notes=? WHERE Id=? AND UserId=?",
                request.ApplicationDate?.Date, request.LotNumber.Trim(), request.Notes.Trim(), existing.Id, userId);
        }

        public async Task RemoveDoseApplicationAsync(int patientId, string doseKey)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            var removed = false;
            await _database.RunInTransactionAsync(connection =>
            {
                var existingRows = ReadDoseApplications(connection, userId, patientId, doseKey);

                if (existingRows.Count > 1)
                {
                    throw new InvalidOperationException("Há aplicações históricas distintas desta dose. Concilie os registros antes de remover.");
                }

                if (existingRows.SingleOrDefault() is { } existing)
                {
                    removed = connection.Delete(existing) > 0;
                }
            });

            if (removed)
            {
                DataChangeTracker.MarkPatientsChanged();
            }
        }

        private async Task<bool> IsPregnantAsync(int patientId, int userId)
        {
            var conditions = await _database.Table<PatientConditions>()
                .Where(condition => condition.UserId == userId && condition.PatientId == patientId)
                .ToListAsync();

            return conditions.Any(IsPregnancy);
        }

        private static bool IsPregnancy(PatientConditions condition) =>
            string.Equals(HealthConditionCatalog.GetKey(condition.Description), HealthConditionCatalog.Gestante,
                StringComparison.OrdinalIgnoreCase);

        private static int GetMonth(DateTime birthDate)
        {
            var today = DateTime.Today;
            var months = (today.Year - birthDate.Year) * 12 + today.Month - birthDate.Month;

            if (today.Day < birthDate.Day)
            {
                months--;
            }

            return Math.Max(months, 0);
        }

        private static PatientVaccineDoseDto BuildDoseDto(
            DateTime birthDate,
            VaccineDoseDefinition definition,
            IReadOnlyDictionary<string, PatientVaccineDose> applicationByDose)
        {
            applicationByDose.TryGetValue(definition.DoseKey, out var application);
            var applicationDate = application?.AppliedAt?.Date;

            var status = VaccineStatusCalculator.Calculate(
                birthDate,
                DateTime.Today,
                definition,
                applicationDate);

            if (application is { IsApplied: true } && !applicationDate.HasValue)
            {
                status = VaccineStatus.Applied;
            }

            return new PatientVaccineDoseDto(
                definition,
                application?.Id,
                VaccineStatusCalculator.GetRecommendedDate(birthDate, definition),
                VaccineStatusCalculator.GetMaximumDate(birthDate, definition),
                applicationDate,
                application?.LotNumber ?? string.Empty,
                application?.Notes ?? string.Empty,
                status);
        }
    }
}
