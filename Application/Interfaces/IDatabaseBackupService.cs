namespace ACS_View.Application.Interfaces;

public interface IDatabaseBackupService
{
    Task<string> CreateSnapshotAsync();
    Task<string> StageRestoreForNextStartAsync(string snapshot);
}
