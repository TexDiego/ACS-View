using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;

namespace ACS_View.UseCases.Services;

internal sealed class ImportHistoryService(IDatabaseService database, ICurrentUserContext user) : IImportHistoryService
{
    public async Task SaveAsync(ImportHistory history)
    {
        await database.InitializeAsync();
        history.UserId = user.RequireCurrentUserId();
        await database.Connection.InsertAsync(history);
    }

    public async Task<List<ImportHistory>> GetAsync()
    {
        await database.InitializeAsync();
        var id = user.RequireCurrentUserId();
        return await database.Connection.Table<ImportHistory>().Where(h => h.UserId == id)
            .OrderByDescending(h => h.Id).ToListAsync();
    }
}
