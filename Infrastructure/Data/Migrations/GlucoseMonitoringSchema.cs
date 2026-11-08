using SQLite;
namespace ACS_View.Infrastructure.Data.Migrations;
internal static class GlucoseMonitoringSchema
{
    internal static void Create(SQLiteConnection c)
    {
        c.Execute("""
            CREATE TABLE IF NOT EXISTS PatientGlucoseMonitoring (
              Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL CHECK(UserId>0), PatientId INTEGER NOT NULL,
              ChecksPerDay INTEGER CHECK(ChecksPerDay>0), VariableFrequency INTEGER NOT NULL DEFAULT 0 CHECK(VariableFrequency IN (0,1)),
              StripsPerPickup INTEGER CHECK(StripsPerPickup>0), PickupIntervalDays INTEGER CHECK(PickupIntervalDays>0),
              LastKnownPickup BIGINT, MonitoringMethod TEXT NOT NULL DEFAULT 'Não informado'
              CHECK(MonitoringMethod IN ('Não informado','Glicosímetro','Sensor contínuo','Ambos')), Notes TEXT NOT NULL DEFAULT '',
              UNIQUE(UserId,PatientId), FOREIGN KEY(PatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("""
            CREATE TABLE IF NOT EXISTS PatientStripPickup (
              Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL CHECK(UserId>0), PatientId INTEGER NOT NULL,
              PickupDate BIGINT NOT NULL, Quantity INTEGER NOT NULL CHECK(Quantity>0), Notes TEXT NOT NULL DEFAULT '',
              FOREIGN KEY(PatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("CREATE INDEX IF NOT EXISTS IX_StripPickup_PatientDate ON PatientStripPickup(UserId,PatientId,PickupDate)");
        foreach(var table in new[]{"PatientGlucoseMonitoring","PatientStripPickup"})
            c.Execute($"CREATE TRIGGER IF NOT EXISTS {table}_OwnerImmutable BEFORE UPDATE OF UserId,PatientId ON {table} WHEN NEW.UserId<>OLD.UserId OR NEW.PatientId<>OLD.PatientId BEGIN SELECT RAISE(ABORT,'Owner cannot change'); END");
    }
}
