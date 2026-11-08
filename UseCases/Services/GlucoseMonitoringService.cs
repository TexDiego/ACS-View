using ACS_View.Application.Interfaces;
using ACS_View.Application.Health;
using CommunityToolkit.Mvvm.Messaging;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
namespace ACS_View.UseCases.Services;

internal sealed class GlucoseMonitoringService(IDatabaseService database, ICurrentUserContext user) : IGlucoseMonitoringService
{
    public async Task<PatientGlucoseMonitoring?> GetAsync(int patientId)
    {
        var owner = user.RequireCurrentUserId();
        return await database.Connection.Table<PatientGlucoseMonitoring>().FirstOrDefaultAsync(x => x.UserId == owner && x.PatientId == patientId);
    }
    public Task<List<PatientStripPickup>> GetPickupsAsync(int patientId)
    {
        var owner = user.RequireCurrentUserId();
        return database.Connection.Table<PatientStripPickup>().Where(x => x.UserId == owner && x.PatientId == patientId).OrderByDescending(x => x.PickupDate).ToListAsync();
    }
    public async Task SaveAsync(int patientId, PatientGlucoseMonitoring monitoring)
    {
        GlucoseMonitoringRules.Validate(monitoring);
        var owner = user.RequireCurrentUserId();
        await database.Connection.RunInTransactionAsync(c =>
        {
            if(user.CurrentUserId != owner) throw new OperationCanceledException("Perfil alterado.");
            EnsurePatient(c, owner, patientId);
            var existing = c.Table<PatientGlucoseMonitoring>().FirstOrDefault(x => x.UserId == owner && x.PatientId == patientId);
            monitoring.Id = existing?.Id ?? 0;
            monitoring.UserId = owner; monitoring.PatientId = patientId;
            if (existing is null) c.Insert(monitoring); else c.Update(monitoring);
        });
        DataChangeTracker.MarkPatientsChanged();
        WeakReferenceMessenger.Default.Send(new PatientHealthChanged(patientId, owner, "diabetes"));
    }
    public async Task AddPickupAsync(int patientId, DateTime date, int quantity, string notes)
    {
        if (quantity <= 0 || date.Date > DateTime.Today || date == default) throw new ArgumentException("Informe uma data até hoje e uma quantidade positiva.");
        var owner = user.RequireCurrentUserId();
        await database.Connection.RunInTransactionAsync(c =>
        {
            if(user.CurrentUserId != owner) throw new OperationCanceledException("Perfil alterado.");
            EnsurePatient(c, owner, patientId);
            c.Insert(new PatientStripPickup { UserId = owner, PatientId = patientId, PickupDate = date.Date, Quantity = quantity, Notes = notes.Trim() });
        });
        DataChangeTracker.MarkPatientsChanged();
        WeakReferenceMessenger.Default.Send(new PatientHealthChanged(patientId, owner, "diabetes"));
    }
    private static void EnsurePatient(SQLite.SQLiteConnection c, int owner, int patientId)
    {
        if (!c.Table<Patient>().Any(x => x.Id == patientId && x.UserId == owner)) throw new InvalidOperationException("Paciente indisponível neste perfil.");
    }
}
