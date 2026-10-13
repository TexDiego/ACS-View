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

            var applicationRows = await ReadApplicationsAsync(userId,patientId,null,true);

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

            var patientExists = await _database.Table<Patient>()
                .CountAsync(patient => patient.Id == request.PatientId && patient.UserId == userId) > 0;

            if (!patientExists)
            {
                throw new InvalidOperationException("Paciente nao encontrado.");
            }

            var existingRows = await ReadApplicationsAsync(userId,request.PatientId,request.DoseKey,false);

            var first = existingRows.OrderBy(dose => dose.Id).FirstOrDefault();
            if (existingRows.Count>1)
                throw new InvalidOperationException("Há aplicações históricas distintas desta dose. Concilie os registros antes de editar.");
            if (first is null)
            {
                await _database.InsertAsync(new PatientVaccineDose
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

                DataChangeTracker.MarkPatientsChanged();
                return;
            }

            first.IsApplied = true;
            first.AppliedAt = request.ApplicationDate?.Date;
            first.LotNumber = request.LotNumber.Trim();
            first.Notes = request.Notes.Trim();
            await _database.ExecuteAsync("UPDATE PatientVaccineDose SET IsApplied=1,AppliedAt=?,LotNumber=?,Notes=? WHERE Id=? AND UserId=?",
                first.AppliedAt,first.LotNumber,first.Notes,first.Id,userId);


            DataChangeTracker.MarkPatientsChanged();
        }

        public async Task RemoveDoseApplicationAsync(int patientId, string doseKey)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            var existingRows = await ReadApplicationsAsync(userId,patientId,doseKey,false);

            if(existingRows.Count>1)
                throw new InvalidOperationException("Há aplicações históricas distintas desta dose. Concilie os registros antes de remover.");

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

        private Task<List<PatientVaccineDose>> ReadApplicationsAsync(int user,int patient,string? key,bool appliedOnly)
        {
            var parameters=new List<object>{user,patient};
            var filter=appliedOnly ? " AND IsApplied=1" : "";
            if(key!=null){filter+=" AND DoseKey=?";parameters.Add(key);}
            return _database.QueryAsync<PatientVaccineDose>($"SELECT {LegacyDateSql.Projection<PatientVaccineDose>()} FROM PatientVaccineDose WHERE UserId=? AND PatientId=?{filter}",parameters.ToArray());
        }
    }
}
