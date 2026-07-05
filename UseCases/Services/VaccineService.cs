using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;

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

            var applicationRows = await _database.Table<PatientVaccineDose>()
                .Where(dose => dose.PatientId == patientId && dose.UserId == userId && dose.IsApplied)
                .ToListAsync();

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

            if (request.ApplicationDate.Date > DateTime.Today)
            {
                throw new InvalidOperationException("A data de aplicacao nao pode ser futura.");
            }

            var patientExists = await _database.Table<Patient>()
                .CountAsync(patient => patient.Id == request.PatientId && patient.UserId == userId) > 0;

            if (!patientExists)
            {
                throw new InvalidOperationException("Paciente nao encontrado.");
            }

            var existingRows = await _database.Table<PatientVaccineDose>()
                .Where(dose => dose.UserId == userId && dose.PatientId == request.PatientId && dose.DoseKey == request.DoseKey)
                .ToListAsync();

            var first = existingRows.OrderBy(dose => dose.Id).FirstOrDefault();
            if (first is null)
            {
                await _database.InsertAsync(new PatientVaccineDose
                {
                    UserId = userId,
                    PatientId = request.PatientId,
                    DoseKey = request.DoseKey,
                    IsApplied = true,
                    AppliedAt = request.ApplicationDate.Date,
                    LotNumber = request.LotNumber.Trim(),
                    Notes = request.Notes.Trim(),
                    CreatedAt = DateTime.Now
                });

                DataChangeTracker.MarkPatientsChanged();
                return;
            }

            first.IsApplied = true;
            first.AppliedAt = request.ApplicationDate.Date;
            first.LotNumber = request.LotNumber.Trim();
            first.Notes = request.Notes.Trim();
            first.CreatedAt = first.CreatedAt == default ? DateTime.Now : first.CreatedAt;
            await _database.UpdateAsync(first);

            foreach (var duplicate in existingRows.Where(dose => dose.Id != first.Id))
            {
                await _database.DeleteAsync(duplicate);
            }

            DataChangeTracker.MarkPatientsChanged();
        }

        public async Task RemoveDoseApplicationAsync(int patientId, string doseKey)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            var existingRows = await _database.Table<PatientVaccineDose>()
                .Where(dose => dose.UserId == userId && dose.PatientId == patientId && dose.DoseKey == doseKey)
                .ToListAsync();

            foreach (var existing in existingRows)
            {
                await _database.DeleteAsync(existing);
            }

            if (existingRows.Count > 0)
            {
                DataChangeTracker.MarkPatientsChanged();
            }
        }

        private async Task<bool> IsPregnantAsync(int patientId, int userId)
        {
            var conditions = await _database.Table<PatientConditions>()
                .Where(condition => condition.UserId == userId && condition.PatientId == patientId)
                .ToListAsync();

            return conditions.Any(condition =>
                string.Equals(
                    HealthConditionCatalog.GetKey(condition.Description),
                    HealthConditionCatalog.Gestante,
                    StringComparison.OrdinalIgnoreCase));
        }

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

            if (application is { IsApplied: true } && !applicationDate.HasValue && application.CreatedAt != default)
            {
                applicationDate = application.CreatedAt.Date;
            }

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
