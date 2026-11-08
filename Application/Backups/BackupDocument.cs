using System.Text.Json;

namespace ACS_View.Application.Backups;

// Relational values retain SQLite integers (including DateTime ticks), nulls and text.
// SQL, triggers, sessions and device credentials are never read from an archive.
public sealed class BackupDocument
{
    public int FormatVersion { get; set; } = 1;
    public int SchemaVersion { get; set; }
    public string AppVersion { get; set; } = "";
    public DateTimeOffset CreatedUtc { get; set; }
    public string ProfileIdentity { get; set; } = "";
    public Dictionary<string, List<Dictionary<string, JsonElement>>> Tables { get; set; } = [];
    public string DashboardPreferences { get; set; } = "";
}

public sealed record BackupSummary(string Username, DateTimeOffset CreatedUtc, IReadOnlyDictionary<string, int> Counts);

public interface IEncryptedBackupService
{
    Task ExportAsync(Stream destination, string currentPassword, string backupPassword, CancellationToken cancellationToken);
    Task<BackupSummary> InspectAsync(Stream source, string password, CancellationToken cancellationToken);
    Task RestoreAsync(bool replaceCurrentProfile, string currentPassword, CancellationToken cancellationToken);
    void Discard();
}
