using SQLite;

namespace ACS_View.Infrastructure.Data.Migrations;

internal static class DatabaseRecovery
{
    internal const string SelectionFile="database-selection.txt";
    public static string GetSelectedDatabasePath(string directory)
    {
        var selection=Path.Combine(directory,SelectionFile);
        if(!File.Exists(selection))return Path.Combine(directory,"health_app.db");
        var name=File.ReadAllText(selection).Trim();
        if(name!=Path.GetFileName(name) || !name.StartsWith("health_app-restored-",StringComparison.Ordinal) || !name.EndsWith(".db",StringComparison.Ordinal))
            throw new InvalidOperationException("Seleção do banco inválida; preserve os arquivos e revise a recuperação.");
        var path=Path.Combine(directory,name);
        if(!File.Exists(path))throw new FileNotFoundException("Banco restaurado não encontrado. Não será criado um banco vazio.");
        return path;
    }
    public static void SelectDatabaseForNextStart(string directory,string restored)
    {
        if(Path.GetDirectoryName(Path.GetFullPath(restored))!=Path.GetFullPath(directory))
            throw new InvalidOperationException("O banco restaurado deve permanecer no diretório privado do app.");
        var selection=Path.Combine(directory,SelectionFile);
        var pending=selection+"."+Guid.NewGuid().ToString("N")+".tmp";
        File.WriteAllText(pending,Path.GetFileName(restored));
        File.Move(pending,selection,true);
    }
    public static async Task<string> CreateSnapshotAsync(SQLiteAsyncConnection source)
    {
        var path = Path.GetFullPath(source.DatabasePath);
        var directory = Path.Combine(Path.GetDirectoryName(path)!, "migration-backups");
        Directory.CreateDirectory(directory);
        var size = new FileInfo(path).Length;
        var drive = new DriveInfo(Path.GetPathRoot(path)!);
        if (drive.AvailableFreeSpace < Math.Max(size * 3, 16 * 1024 * 1024))
            throw new IOException("Espaço insuficiente para snapshot e migração.");
        var snapshot = Path.Combine(directory, $"pre-migration-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.db");
        for(var attempt=0;;attempt++)
        {
            try { await source.BackupAsync(snapshot);break; }
            catch(SQLiteException ex) when(attempt<3 && ex.Result is SQLite3.Result.Busy or SQLite3.Result.Locked)
            { await Task.Delay(50*(attempt+1)); }
        }
        using var check = new SQLiteConnection(snapshot, SQLiteOpenFlags.ReadOnly);
        SchemaMigrator.ValidateIntegrity(check);
        return snapshot;
    }

    // Restore into a NEW path. Caller closes the app/connections before selecting this path.
    // Never overwrite the original or its WAL/SHM, which remain available for recovery.
    public static string RestoreToNewPath(string snapshot, string destination)
    {
        if (File.Exists(destination)) throw new IOException("Destino da restauração já existe.");
        using var source = new SQLiteConnection(snapshot, SQLiteOpenFlags.ReadOnly);
        SchemaMigrator.ValidateIntegrity(source);
        if (source.ExecuteScalar<int>("PRAGMA user_version") > SchemaMigrator.CurrentVersion)
            throw new InvalidOperationException("Backup de versão mais recente.");
        var drive=new DriveInfo(Path.GetPathRoot(Path.GetFullPath(destination))!);
        if(drive.AvailableFreeSpace < Math.Max(new FileInfo(snapshot).Length*3,16*1024*1024))
            throw new IOException("Espaço insuficiente para restaurar e validar o backup.");
        source.Backup(destination);
        using var restored = new SQLiteConnection(destination, SQLiteOpenFlags.ReadOnly);
        SchemaMigrator.ValidateIntegrity(restored);
        SchemaMigrator.ValidatePreservation(restored, SchemaMigrator.Capture(source));
        return destination;
    }
}
