using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;
using ACS_View.Infrastructure.Data.Migrations;

namespace ACS_View.UseCases.Services;

internal sealed class PregnancyService(
    IDatabaseService databaseService,
    ICurrentUserContext currentUserContext) : IPregnancyService
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public async Task<PatientPregnancy?> GetActiveOrLatestByPatientIdAsync(int patientId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var pregnancies = await ReadPregnanciesAsync(userId,patientId);

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

        var registeredChildrenCount = await _connection.Table<Patient>()
            .Where(item => item.UserId == userId && item.MotherPatientId == patientId)
            .CountAsync();
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
            Patient = patient,
            Pregnancy = pregnancy,
            RegisteredChildrenCount = registeredChildrenCount,
            ConditionDescriptions = conditions.Select(condition => condition.Description ?? string.Empty).ToList(),
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

        await _connection.RunInTransactionAsync(c=>
        {
            if(c.Table<Patient>().FirstOrDefault(p=>p.UserId==pregnancy.UserId && p.Id==pregnancy.PatientId)==null)
                throw new InvalidOperationException("Paciente não pertence à conta atual.");
            if(pregnancy.Id>0)
            {
                var original=c.Table<PatientPregnancy>().FirstOrDefault(p=>p.Id==pregnancy.Id && p.UserId==pregnancy.UserId && p.PatientId==pregnancy.PatientId);
                if(original==null)throw new InvalidOperationException("Episódio não pertence ao paciente atual.");
                c.Execute("UPDATE PatientPregnancy SET LastMenstrualPeriod=?,ExpectedBirthDate=?,Status=?,ManualRisk=?,InformedChildrenCount=?,EndedAt=?,EndType=?,Notes=? WHERE Id=? AND UserId=?",
                    pregnancy.LastMenstrualPeriod,pregnancy.ExpectedBirthDate,pregnancy.Status,pregnancy.ManualRisk,pregnancy.InformedChildrenCount,pregnancy.EndedAt,pregnancy.EndType,pregnancy.Notes,pregnancy.Id,pregnancy.UserId);
            }
            else
            {
                pregnancy.CreatedAt=pregnancy.CreatedAt==default?DateTime.UtcNow:pregnancy.CreatedAt;
                c.Insert(pregnancy);
            }
        });

        DataChangeTracker.MarkPatientsChanged();
        return pregnancy;
    }

    private Task<List<PatientPregnancy>> ReadPregnanciesAsync(int user,int patient) =>
        _connection.QueryAsync<PatientPregnancy>($"SELECT {LegacyDateSql.Projection<PatientPregnancy>()} FROM PatientPregnancy WHERE UserId=? AND PatientId=?",user,patient);

    public async Task SyncPregnancyConditionAsync(int patientId, bool isPregnant)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var active=(await ReadPregnanciesAsync(userId,patientId)).Where(p=>p.Status==PregnancyStatus.Active).ToList();
        if(!isPregnant && active.Count>1)
            throw new InvalidOperationException("Há episódios gestacionais ativos distintos. Concilie os episódios antes de desmarcar a condição.");
        var activePregnancy=active.FirstOrDefault();

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
