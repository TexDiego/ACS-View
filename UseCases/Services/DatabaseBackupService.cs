using ACS_View.Application.Interfaces;
using ACS_View.Infrastructure.Data.Migrations;
using SQLite;

namespace ACS_View.UseCases.Services;

internal sealed class DatabaseBackupService(IDatabaseService database) : IDatabaseBackupService
{
    private readonly SemaphoreSlim restoreLock=new(1,1);
    public async Task<string> CreateSnapshotAsync()
    {
        await database.InitializeAsync();
        return await DatabaseRecovery.CreateSnapshotAsync(database.Connection);
    }
    public async Task<string> StageRestoreForNextStartAsync(string snapshot)
    {
        await restoreLock.WaitAsync();
        try
        {
            var directory=Path.GetDirectoryName(Path.GetFullPath(database.Connection.DatabasePath))!;
            var destination=Path.Combine(directory,$"health_app-restored-{Guid.NewGuid():N}.db");
            DatabaseRecovery.RestoreToNewPath(snapshot,destination);
            var restored=new SQLiteAsyncConnection(destination);
            try { await new SchemaMigrator(restored).RunAsync(); }
            finally { await restored.CloseAsync(); }
            // Switch only after verification, on the next process startup. The live DB remains.
            DatabaseRecovery.SelectDatabaseForNextStart(directory,destination);
            return destination;
        }
        finally { restoreLock.Release(); }
    }
}
