using System.Text.Json;
using ACS_View.Application.Backups;
using ACS_View.Application.Interfaces;
using ACS_View.Infrastructure.Data.Migrations;
using SQLite;

namespace ACS_View.UseCases.Services;

internal sealed class EncryptedBackupService(IDatabaseService database, IDatabaseBackupService snapshots,
    ICurrentUserContext context, IAuthService auth) : IEncryptedBackupService
{
    private static readonly SemaphoreSlim gate = new(1, 1);
    private BackupDocument? inspected;
    public void Discard() => inspected = null;

    public async Task ExportAsync(Stream destination, string currentPassword, string backupPassword, CancellationToken cancellationToken)
    {
        if (!await gate.WaitAsync(0, cancellationToken)) throw new InvalidOperationException("Um backup já está em andamento.");
        try
        {
            var id = context.RequireCurrentUserId();
            await auth.VerifyCurrentPasswordAsync(currentPassword);
            BackupEncryptionService.ValidatePassword(backupPassword);
            if (backupPassword == currentPassword) throw new InvalidOperationException("Escolha uma senha de backup diferente da senha da conta.");
            await database.InitializeAsync();
            var snapshot = await snapshots.CreateSnapshotAsync(); // retained internal recovery point
            var document = await Task.Run(() =>
            {
                using var c = new SQLiteConnection(snapshot, SQLiteOpenFlags.ReadOnly);
                return UserBackupData.Extract(c, id, AppInfo.Current.VersionString, Preferences.Get($"DashboardMetricPreferences.{id}", ""));
            }, cancellationToken);
            if (context.CurrentUserId != id) throw new InvalidOperationException("A sessão mudou. Inicie a exportação novamente.");
            await Task.Run(() => BackupEncryptionService.EncryptAsync(document, destination, backupPassword, cancellationToken), cancellationToken);
        }
        finally { gate.Release(); }
    }

    public async Task<BackupSummary> InspectAsync(Stream source, string password, CancellationToken cancellationToken)
    {
        if (!await gate.WaitAsync(0, cancellationToken)) throw new InvalidOperationException("Um backup já está em andamento.");
        try
        {
            inspected = null;
            var document = await Task.Run(() => BackupEncryptionService.DecryptAsync(source, password, cancellationToken), cancellationToken);
            await Task.Run(() => UserBackupData.Validate(document), cancellationToken);
            inspected = document;
            return UserBackupData.Summary(document);
        }
        finally { gate.Release(); }
    }

    public async Task RestoreAsync(bool replaceCurrentProfile, string currentPassword, CancellationToken cancellationToken)
    {
        if (!await gate.WaitAsync(0, cancellationToken)) throw new InvalidOperationException("Um backup já está em andamento.");
        string? prepared = null;
        try
        {
            var document = inspected ?? throw new InvalidOperationException("Selecione e valide o backup novamente.");
            int? replace = null;
            if (replaceCurrentProfile)
            {
                replace = context.RequireCurrentUserId();
                await auth.VerifyCurrentPasswordAsync(currentPassword);
                var user = await auth.GetCurrentUserAsync();
                if (user is null || !string.Equals(user.Username.Trim(), UserBackupData.Summary(document).Username.Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("A substituição exige autenticação no perfil com o mesmo nome do backup.");
            }
            await database.InitializeAsync();
            if (await database.Connection.ExecuteScalarAsync<int>("PRAGMA query_only") != 0
                || File.Exists(Path.Combine(Path.GetDirectoryName(database.Connection.DatabasePath)!, "profile-backup-activation.json")))
                throw new InvalidOperationException("Já existe uma restauração preparada. Feche completamente e abra o aplicativo.");
            var snapshot = await DatabaseRecovery.CreateSnapshotAsync(database.Connection);
            var directory = Path.GetDirectoryName(database.Connection.DatabasePath)!;
            prepared = Path.Combine(directory, $"health_app-restored-{Guid.NewGuid():N}.db");
            DatabaseRecovery.RestoreToNewPath(snapshot, prepared);
            var connection = new SQLiteAsyncConnection(prepared);
            int restoredId = 0;
            List<SchemaMigrator.TableSnapshot> before;
            using (var c = new SQLiteConnection(snapshot, SQLiteOpenFlags.ReadOnly)) before = SchemaMigrator.Capture(c);
            try
            {
                await new SchemaMigrator(connection).RunAsync();
                await connection.RunInTransactionAsync(c =>
                {
                    var preserved = CaptureExcept(c, replace);
                    restoredId = UserBackupData.Merge(c, document, replace, cancellationToken);
                    SchemaMigrator.ValidatePreservation(c, preserved);
                    // New path invalidates Android grants; fresh identity invalidates Windows grants.
                    c.Execute("CREATE TABLE IF NOT EXISTS LocalAuthIdentity (Id INTEGER PRIMARY KEY CHECK(Id=1), Identity TEXT NOT NULL)");
                    c.Execute("INSERT OR REPLACE INTO LocalAuthIdentity VALUES(1,?)", Guid.NewGuid().ToString("N"));
                });
            }
            finally { await connection.CloseAsync(); }
            cancellationToken.ThrowIfCancellationRequested();
            // Settings travel with the prepared DB. Startup applies them only for the selected
            // path, and only after DB initialization. Failed selection cannot touch preferences.
            File.WriteAllText(prepared + ".profile-settings", JsonSerializer.Serialize(new RestoredSettings(restoredId, document.DashboardPreferences)));
            // Final read transaction serializes all app writes. Reject stale preparation rather
            // than lose changes committed after the snapshot. Then freeze this connection until
            // restart: activation never overwrites a live SQLite/WAL/SHM file.
            await database.Connection.RunInTransactionAsync(c =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var now = SchemaMigrator.Capture(c);
                if (!Equivalent(before, now)) throw new InvalidOperationException("Os dados mudaram durante a preparação. Tente restaurar novamente.");
                if (replace.HasValue && context.CurrentUserId != replace.Value) throw new InvalidOperationException("A sessão mudou. Restauração cancelada.");
                c.Execute("PRAGMA query_only=ON");
                try { DatabaseRecovery.SelectProfileBackupForNextStart(directory, prepared, database.Connection.DatabasePath); }
                catch { c.Execute("PRAGMA query_only=OFF"); throw; }
            });
            prepared = null; inspected = null;
            context.Clear();
            // No device vault is imported. Existing users' enrollment files are untouched.
            try { SecureStorage.Default.Remove("LocalAuthSessionV2"); } catch { /* New DB path/identity already invalidates the session. */ }
        }
        finally
        {
            if (prepared is not null)
                foreach (var suffix in new[] { "", "-wal", "-shm", ".profile-settings" })
                    try { File.Delete(prepared + suffix); } catch (IOException) { }
            gate.Release();
        }
    }

    private static bool Equivalent(List<SchemaMigrator.TableSnapshot> a, List<SchemaMigrator.TableSnapshot> b) =>
        a.Count == b.Count && a.Zip(b).All(pair => pair.First.Name == pair.Second.Name && pair.First.Columns.SequenceEqual(pair.Second.Columns) && pair.First.Rows.SequenceEqual(pair.Second.Rows));

    private static List<SchemaMigrator.TableSnapshot> CaptureExcept(SQLiteConnection c, int? replace)
    {
        if (!replace.HasValue) return SchemaMigrator.Capture(c);
        // Fingerprint only the untouched rows, without deleting/modifying live records.
        var savepoint = "backup_preserve";
        c.Execute($"SAVEPOINT {savepoint}");
        var triggers = c.Query<Trigger>("SELECT name,sql FROM sqlite_master WHERE type='trigger'");
        foreach (var t in triggers) c.Execute($"DROP TRIGGER {SchemaMigrator.Quote(t.Name)}");
        c.Execute("PRAGMA defer_foreign_keys=ON");
        foreach (var t in UserBackupData.OwnedTables.Reverse()) c.Execute($"DELETE FROM {SchemaMigrator.Quote(t)} WHERE {(t == "User" ? "Id" : "UserId")}=?", replace.Value);
        var result = SchemaMigrator.Capture(c);
        c.Execute($"ROLLBACK TO {savepoint}"); c.Execute($"RELEASE {savepoint}");
        return result;
    }
    private sealed class Trigger { public string Name { get; set; } = ""; public string Sql { get; set; } = ""; }
    internal sealed record RestoredSettings(int UserId, string DashboardPreferences);
    internal static void ApplyRestoredSettings(string databasePath)
    {
        var path = databasePath + ".profile-settings";
        if (!File.Exists(path)) return;
        var settings = JsonSerializer.Deserialize<RestoredSettings>(File.ReadAllText(path)) ?? throw new InvalidDataException("Configurações restauradas inválidas.");
        Preferences.Set($"DashboardMetricPreferences.{settings.UserId}", settings.DashboardPreferences);
        Preferences.Set("BackupRequirePasswordOnce", "1");
        File.Delete(path);
    }
}
