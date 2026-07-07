using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.UseCases.Services;

internal sealed class PregnancyService(
    IDatabaseService databaseService,
    ICurrentUserContext currentUserContext) : IPregnancyService
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public async Task<PatientPregnancy?> GetActiveOrLatestByPatientIdAsync(int patientId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var pregnancies = await _connection.Table<PatientPregnancy>()
            .Where(pregnancy => pregnancy.UserId == userId && pregnancy.PatientId == patientId)
            .ToListAsync();

        return pregnancies
            .OrderByDescending(pregnancy => PregnancyCalculator.IsPuerperal(pregnancy))
            .ThenByDescending(pregnancy => pregnancy.Status == PregnancyStatus.Active)
            .ThenByDescending(pregnancy => pregnancy.CreatedAt)
            .ThenByDescending(pregnancy => pregnancy.Id)
            .FirstOrDefault();
    }

    public async Task<PregnancyDetailsDto?> GetDetailsByPatientIdAsync(int patientId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var patient = await _connection.Table<Patient>()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.Id == patientId);

        if (patient is null)
        {
            return null;
        }

        var pregnancy = await GetActiveOrLatestByPatientIdAsync(patientId);
        if (pregnancy is null)
        {
            return null;
        }

        var allPatients = await _connection.Table<Patient>()
            .Where(item => item.UserId == userId)
            .ToListAsync();
        var registeredChildrenCount = PregnancyCalculator.CalculateRegisteredChildrenCount(patientId, allPatients);
        var conditions = await _connection.Table<PatientConditions>()
            .Where(condition => condition.UserId == userId && condition.PatientId == patientId)
            .ToListAsync();
        var suggestion = PregnancyRiskSuggestionCalculator.Calculate(
            patient,
            pregnancy,
            conditions.Select(condition => condition.Description ?? string.Empty),
            registeredChildrenCount);

        return new PregnancyDetailsDto
        {
            Pregnancy = pregnancy,
            RegisteredChildrenCount = registeredChildrenCount,
            RiskSuggestion = suggestion,
            GestationalAge = PregnancyCalculator.CalculateGestationalAge(pregnancy),
            Trimester = PregnancyCalculator.CalculateTrimester(pregnancy),
            IsPuerperal = PregnancyCalculator.IsPuerperal(pregnancy),
            PostpartumDays = PregnancyCalculator.CalculatePostpartumDays(pregnancy),
            PuerperiumEndDate = PregnancyCalculator.CalculatePuerperiumEndDate(pregnancy)
        };
    }

    public async Task<PatientPregnancy> SaveAsync(PatientPregnancy pregnancy)
    {
        pregnancy.UserId = currentUserContext.RequireCurrentUserId();
        pregnancy.Notes ??= string.Empty;

        if (pregnancy.LastMenstrualPeriod is not null &&
            pregnancy.ExpectedBirthDate is null)
        {
            pregnancy.ExpectedBirthDate = PregnancyCalculator.CalculateExpectedBirthDate(pregnancy.LastMenstrualPeriod.Value);
        }

        if (pregnancy.Status == PregnancyStatus.Ended && pregnancy.EndedAt is null)
        {
            pregnancy.EndedAt = DateTime.Today;
        }

        if (pregnancy.Id > 0)
        {
            await _connection.UpdateAsync(pregnancy);
        }
        else
        {
            pregnancy.CreatedAt = pregnancy.CreatedAt == default ? DateTime.UtcNow : pregnancy.CreatedAt;
            await _connection.InsertAsync(pregnancy);
        }

        if (pregnancy.Status != PregnancyStatus.Active)
        {
            await CloseOtherActivePregnanciesAsync(pregnancy);
        }

        DataChangeTracker.MarkPatientsChanged();
        return pregnancy;
    }

    private async Task CloseOtherActivePregnanciesAsync(PatientPregnancy pregnancy)
    {
        var activePregnancies = await _connection.Table<PatientPregnancy>()
            .Where(item =>
                item.UserId == pregnancy.UserId &&
                item.PatientId == pregnancy.PatientId &&
                item.Status == PregnancyStatus.Active)
            .ToListAsync();

        foreach (var activePregnancy in activePregnancies.Where(item => item.Id != pregnancy.Id))
        {
            activePregnancy.Status = PregnancyStatus.NotConfirmed;
            activePregnancy.EndedAt = null;
            activePregnancy.EndType = null;
            await _connection.UpdateAsync(activePregnancy);
        }
    }

    public async Task SyncPregnancyConditionAsync(int patientId, bool isPregnant)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var activePregnancy = await _connection.Table<PatientPregnancy>()
            .FirstOrDefaultAsync(pregnancy =>
                pregnancy.UserId == userId &&
                pregnancy.PatientId == patientId &&
                pregnancy.Status == PregnancyStatus.Active);

        if (isPregnant)
        {
            if (activePregnancy is not null)
            {
                return;
            }

            await SaveAsync(new PatientPregnancy
            {
                UserId = userId,
                PatientId = patientId,
                Status = PregnancyStatus.Active,
                ManualRisk = PregnancyRisk.NotInformed,
                CreatedAt = DateTime.UtcNow
            });
            return;
        }

        if (activePregnancy is null)
        {
            return;
        }

        activePregnancy.Status = PregnancyStatus.NotConfirmed;
        activePregnancy.EndedAt = null;
        activePregnancy.EndType = null;
        await SaveAsync(activePregnancy);
    }
}
