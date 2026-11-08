using ACS_View.Application.Interfaces;
using ACS_View.Application.Health;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.Messaging;
namespace ACS_View.UseCases.Services;

internal sealed class PatientInfectiousFollowUpService(IDatabaseService database, ICurrentUserContext user) : IPatientInfectiousFollowUpService
{
    public async Task<PatientInfectiousFollowUp?> GetAsync(int patientId, string conditionCode)
    {
        InfectiousFollowUpRules.Statuses(conditionCode);
        var owner = user.RequireCurrentUserId();
        return await database.Connection.Table<PatientInfectiousFollowUp>().FirstOrDefaultAsync(x => x.UserId == owner && x.PatientId == patientId && x.ConditionCode == conditionCode);
    }
    public async Task SaveAsync(int patientId, PatientInfectiousFollowUp value)
    {
        InfectiousFollowUpRules.Validate(value);
        var owner = user.RequireCurrentUserId();
        await database.Connection.RunInTransactionAsync(c =>
        {
            if(user.CurrentUserId != owner) throw new OperationCanceledException("Perfil alterado.");
            if(!c.Table<Patient>().Any(x => x.Id == patientId && x.UserId == owner)) throw new InvalidOperationException("Paciente indisponível neste perfil.");
            var existing = c.Table<PatientInfectiousFollowUp>().FirstOrDefault(x => x.UserId == owner && x.PatientId == patientId && x.ConditionCode == value.ConditionCode);
            value.Id = existing?.Id ?? 0; value.UserId = owner; value.PatientId = patientId;
            if(existing is null) c.Insert(value); else c.Update(value);
        });
        DataChangeTracker.MarkPatientsChanged();
        WeakReferenceMessenger.Default.Send(new PatientHealthChanged(patientId, owner, value.ConditionCode));
    }
}
