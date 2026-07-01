using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLitePatientInsulinDependencyRepository(
    IDatabaseService databaseService,
    ICurrentUserContext currentUserContext) : IPatientInsulinDependencyRepository
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public Task<PatientInsulinDependency?> GetByPatientIdAsync(int patientId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<PatientInsulinDependency>()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.PatientId == patientId);
    }

    public async Task UpsertAsync(int patientId)
    {
        var existing = await GetByPatientIdAsync(patientId);
        if (existing is not null)
        {
            return;
        }

        await _connection.InsertAsync(new PatientInsulinDependency
        {
            UserId = currentUserContext.RequireCurrentUserId(),
            PatientId = patientId
        });
        DataChangeTracker.MarkPatientsChanged();
    }

    public async Task DeleteByPatientIdAsync(int patientId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        await _connection.Table<PatientInsulinDependency>()
            .Where(item => item.UserId == userId && item.PatientId == patientId)
            .DeleteAsync();
        DataChangeTracker.MarkPatientsChanged();
    }
}
