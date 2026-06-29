using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
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

            var appliedRows = await _database.Table<PatientVaccineDose>()
                .Where(dose => dose.PatientId == patientId && dose.UserId == userId && dose.IsApplied)
                .ToListAsync();

            var appliedDoses = appliedRows
                .GroupBy(dose => dose.DoseKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Any(dose => dose.IsApplied), StringComparer.OrdinalIgnoreCase);

            var isPregnant = await IsPregnantAsync(patientId, userId);
            var ageMonths = GetMonth(patient.BirthDate);
            var doses = VaccineDoseCatalog
                .GetApplicableDefinitions(ageMonths, isPregnant)
                .Select(definition => new PatientVaccineDoseDto(
                    definition,
                    appliedDoses.TryGetValue(definition.DoseKey, out var isApplied) && isApplied))
                .ToList();

            return new PatientVaccineScheduleDto
            {
                PatientId = patientId,
                BirthDate = patient.BirthDate,
                IsPregnant = isPregnant,
                AppliedDoses = appliedDoses,
                Doses = doses
            };
        }

        public async Task SetDoseStatusAsync(int patientId, string doseKey, bool isApplied)
        {
            var userId = currentUserContext.RequireCurrentUserId();

            if (VaccineDoseCatalog.GetDefinition(doseKey) is null)
            {
                throw new ArgumentException("Dose de vacina inválida.", nameof(doseKey));
            }

            var patientExists = await _database.Table<Patient>()
                .CountAsync(patient => patient.Id == patientId && patient.UserId == userId) > 0;

            if (!patientExists)
            {
                throw new InvalidOperationException("Paciente não encontrado.");
            }

            var existingRows = await _database.Table<PatientVaccineDose>()
                .Where(dose => dose.UserId == userId && dose.PatientId == patientId && dose.DoseKey == doseKey)
                .ToListAsync();

            if (!isApplied)
            {
                foreach (var existing in existingRows)
                {
                    await _database.DeleteAsync(existing);
                }

                return;
            }

            var first = existingRows.OrderBy(dose => dose.Id).FirstOrDefault();
            if (first is null)
            {
                await _database.InsertAsync(new PatientVaccineDose
                {
                    UserId = userId,
                    PatientId = patientId,
                    DoseKey = doseKey,
                    IsApplied = true,
                    AppliedAt = DateTime.Now
                });

                return;
            }

            first.IsApplied = true;
            first.AppliedAt ??= DateTime.Now;
            await _database.UpdateAsync(first);

            foreach (var duplicate in existingRows.Where(dose => dose.Id != first.Id))
            {
                await _database.DeleteAsync(duplicate);
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
    }
}
