using System.Security.Cryptography;
using System.Text;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.Infrastructure.Data.Migrations;

internal sealed class SchemaMigrator(SQLiteAsyncConnection database)
{
    public const int CurrentVersion = 5;
    // Only invoked from startup before services may write. SQLite serializes each transaction.
    internal Action<int>? BeforeValidation { get; init; }
    public string? BackupPath { get; private set; }

    public async Task RunAsync()
    {
        await database.ExecuteAsync("PRAGMA foreign_keys = ON");
        if (await database.ExecuteScalarAsync<int>("PRAGMA foreign_keys") != 1)
            throw new InvalidOperationException("SQLite foreign keys unavailable.");
        var version = await database.ExecuteScalarAsync<int>("PRAGMA user_version");
        if (version > CurrentVersion) throw new InvalidOperationException("Banco de versão mais recente. Use a versão compatível do aplicativo.");
        var tables = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'");
        if (tables > 0 && version < CurrentVersion)
            BackupPath = await DatabaseRecovery.CreateSnapshotAsync(database);

        await database.RunInTransactionAsync(c =>
        {
            // Another startup may have completed while this connection waited for the lock.
            version=c.ExecuteScalar<int>("PRAGMA user_version");
            if(version>CurrentVersion)throw new InvalidOperationException("Banco de versão mais recente.");
            ValidateIntegrity(c);
            var before = version<CurrentVersion ? Capture(c) : [];
            c.Execute("CREATE TABLE IF NOT EXISTS SchemaMigration (Version INTEGER PRIMARY KEY, Name TEXT NOT NULL, CompletedAt TEXT NOT NULL, PreservationDigest TEXT NOT NULL)");
            c.Execute("CREATE TABLE IF NOT EXISTS SchemaPreservation (MigrationVersion INTEGER NOT NULL, TableName TEXT NOT NULL, OriginalCount INTEGER NOT NULL, FinalCount INTEGER NOT NULL, OriginalColumns TEXT NOT NULL, OriginalDigest TEXT NOT NULL, PRIMARY KEY(MigrationVersion,TableName))");
            // A committed version must have a contiguous ledger. Rolled back attempts have neither.
            var completed = c.Query<MigrationVersion>("SELECT Version FROM SchemaMigration ORDER BY Version");
            if (!completed.Select(x=>x.Version).SequenceEqual(Enumerable.Range(1,version)))
                throw new InvalidOperationException("Registro de migrações inconsistente; restaure um snapshot validado.");
            if(version==CurrentVersion)
            {
                RelationalStore.ValidateProjection(c);
                return;
            }
            for (var next = version + 1; next <= CurrentVersion; next++)
            {
                switch (next)
                {
                    case 1: ExpandLegacy(c); break;
                    case 2: RelationalStore.CreateSchema(c); break;
                    case 3: RelationalStore.MigrateData(c,before.Any(t=>t.Name=="PatientConditions" && t.Columns.Contains("ConditionId"))); break;
                    case 4: RelationalStore.BackfillSusNumbers(c); break;
                    case 5: AddHouseTerritory(c, before); break;
                }
                BeforeValidation?.Invoke(next);
                ValidatePreservation(c, before);
                ValidateIntegrity(c);
                foreach(var t in before)
                    c.Execute("INSERT INTO SchemaPreservation VALUES(?,?,?,?,?,?)",next,t.Name,t.Rows.Length,
                        c.ExecuteScalar<int>($"SELECT COUNT(*) FROM {Quote(t.Name)}"),string.Join(",",t.Columns),Digest([t]));
                c.Execute("INSERT INTO SchemaMigration VALUES (?, ?, ?, ?)", next,
                    new[] { "", "legacy-expand", "relational-expand", "relational-backfill", "sus-entity-backfill", "house-territory" }[next],
                    DateTime.UtcNow.ToString("O"), Digest(before));
                c.Execute($"PRAGMA user_version = {next}");
            }
            ValidatePreservation(c, before);
            RelationalStore.ValidateProjection(c);
        });
    }

    internal static void ValidateIntegrity(SQLiteConnection c)
    {
        if (c.ExecuteScalar<string>("PRAGMA integrity_check") != "ok" || c.Query<ForeignKeyFailure>("PRAGMA foreign_key_check").Count != 0)
            throw new InvalidOperationException("Falha na integridade SQLite. Banco original preservado.");
    }

    private sealed class ForeignKeyFailure { public string table { get; set; } = ""; }
    private sealed class TableName { public string Name { get; set; } = ""; }
    private sealed class MigrationVersion { public int Version { get; set; } }
    private sealed class FingerprintRow { public string Value { get; set; } = ""; }
    internal sealed record TableSnapshot(string Name, string[] Columns, string[] Rows);

    internal static List<TableSnapshot> Capture(SQLiteConnection c) =>
        c.Query<TableName>("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
            .Select(t => { var columns = c.GetTableInfo(t.Name).Select(x => x.Name).ToArray();
                return new TableSnapshot(t.Name, columns, Fingerprints(c, t.Name, columns)); }).ToList();

    private static string[] Fingerprints(SQLiteConnection c, string table, string[] columns)
    {
        // hex preserves embedded NULs, unlike SQLite quote(TEXT). Include storage type.
        var expression = string.Join(" || ", columns.Select(n => $"typeof({Quote(n)}) || ':' || length(hex(CAST({Quote(n)} AS BLOB))) || ':' || hex(CAST({Quote(n)} AS BLOB)) || ';'"));
        return c.Query<FingerprintRow>($"SELECT {expression} AS Value FROM {Quote(table)}")
            .Select(r => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(r.Value))))
            .Order(StringComparer.Ordinal).ToArray();
    }
    internal static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
    internal static void ValidatePreservation(SQLiteConnection c, List<TableSnapshot> before)
    {
        foreach (var t in before)
        {
            var after=Fingerprints(c,t.Name,t.Columns).GroupBy(x=>x).ToDictionary(g=>g.Key,g=>g.Count());
            if (t.Rows.GroupBy(x => x).Any(g => !after.TryGetValue(g.Key,out var count) || count < g.Count()))
                throw new InvalidOperationException($"Preservação falhou na tabela {t.Name}; migração revertida.");
        }
    }
    private static string Digest(List<TableSnapshot> before) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(string.Join("\n", before.Select(t => t.Name + ":" + string.Join(",", t.Rows))))));

    private static void AddHouseTerritory(SQLiteConnection c, List<TableSnapshot> before)
    {
        var originalColumns = before.FirstOrDefault(t => t.Name == "House")?.Columns ?? [];
        var columns = new[] { (Name: "SetorCensitario", Source: "CensusSector"), (Name: "NumeroQuadra", Source: "BlockNumber") };
        foreach (var column in columns)
            if (!c.GetTableInfo("House").Any(x => x.Name == column.Name))
                c.Execute($"ALTER TABLE House ADD COLUMN {Quote(column.Name)} TEXT NULL");
        // Populate only newly introduced columns from the existing active history. The migration
        // holds the write transaction; suspend the revision trigger so original revisions remain exact.
        var revisionTrigger = c.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE type='trigger' AND name='House_Revision'");
        if (!string.IsNullOrEmpty(revisionTrigger)) c.Execute("DROP TRIGGER House_Revision");
        foreach (var column in columns.Where(x => !originalColumns.Contains(x.Name)))
            c.Execute($"UPDATE House SET {Quote(column.Name)}=(SELECT NULLIF(t.{column.Source},'') FROM TerritorialAssignment t WHERE t.HouseId=House.CasaId AND t.UserId=House.UserId AND t.ValidTo IS NULL) WHERE EXISTS (SELECT 1 FROM TerritorialAssignment t WHERE t.HouseId=House.CasaId AND t.UserId=House.UserId AND t.ValidTo IS NULL)");
        if (!string.IsNullOrEmpty(revisionTrigger)) c.Execute(revisionTrigger);
        c.Execute("CREATE INDEX IF NOT EXISTS IX_House_User_Active_Territory ON House(UserId,IsActive,SetorCensitario,NumeroQuadra)");
    }

    private static void ExpandLegacy(SQLiteConnection c)
    {
        var types = new[] { typeof(Patient), typeof(House), typeof(Family), typeof(User), typeof(Note), typeof(Visits),
            typeof(VisitBatch), typeof(VisitCareLine), typeof(Vaccines), typeof(PatientVaccineDose), typeof(PatientBolsaFamilia),
            typeof(PatientInsulinDependency), typeof(PatientPregnancy), typeof(CareNotification), typeof(PatientCID),
            typeof(PatientConditions), typeof(CidCategory), typeof(CidChapter), typeof(CidGroup), typeof(CidSubcategory),
            typeof(CommonConditions), typeof(CepAddressCache), typeof(ImportHistory) };
        foreach (var type in types)
        {
            var table = type.Name;
            var existing = c.GetTableInfo(table);
            if (existing.Count > 0)
            {
                // sqlite-net normally adds missing columns without defaults; explicit defaults
                // here keep pre-UserId rows quarantined under 0 and boolean status compatible.
                foreach (var property in type.GetProperties().Where(p => p.CanWrite && !Attribute.IsDefined(p, typeof(IgnoreAttribute))))
                {
                    if (existing.Any(x => x.Name == property.Name)) continue;
                    var nullable = Nullable.GetUnderlyingType(property.PropertyType);
                    var valueType = nullable ?? property.PropertyType;
                    var definition = valueType == typeof(string) ? "TEXT NULL" : valueType == typeof(DateTime) ? "BIGINT NULL" : "INTEGER NULL";
                    if (nullable == null && (valueType == typeof(int) || valueType == typeof(bool) || valueType == typeof(long) || valueType.IsEnum))
                        definition = "INTEGER NOT NULL DEFAULT " + (property.Name == "IsActive" ? "1" : property.Name is "HouseId" or "FamilyId" ? "-1" : "0");
                    c.Execute($"ALTER TABLE {Quote(table)} ADD COLUMN {Quote(property.Name)} {definition}");
                    if (table == "Visits" && property.Name == "VisitDate")
                        c.Execute($"UPDATE Visits SET {Quote(property.Name)}=Date");
                    if (table == "Visits" && property.Name == "Status")
                        c.Execute("UPDATE Visits SET Status=CASE Description COLLATE NOCASE WHEN 'Realizada' THEN 'Completed' WHEN 'Ausente' THEN 'Absent' WHEN 'Recusada' THEN 'Refused' ELSE '' END");
                }
            }
            c.CreateTable(type);
            if(table=="Patient")
                foreach(var p in c.Table<Patient>().ToList())
                    foreach(var search in new[]{("SearchName",p.Name),("SearchMotherName",p.MotherName),("SearchFatherName",p.FatherName)})
                        if(!existing.Any(x=>x.Name==search.Item1))
                            c.Execute($"UPDATE Patient SET {search.Item1}=? WHERE Id=?",SearchTextNormalizer.Normalize(search.Item2),p.Id);
            if(table=="House")
                foreach(var h in c.Table<House>().ToList())
                    foreach(var search in new[]{("SearchRua",string.Join(" ",new[]{h.TipoLogradouro,h.Rua}.Where(x=>!string.IsNullOrWhiteSpace(x)))),("SearchComplemento",h.Complemento)})
                        if(!existing.Any(x=>x.Name==search.Item1))
                            c.Execute($"UPDATE House SET {search.Item1}=? WHERE CasaId=?",SearchTextNormalizer.Normalize(search.Item2),h.CasaId);
        }
        // Earlier sqlite-net expansions could leave a nullable UserId column. Preserve
        // its original storage while exposing unknown ownership as 0 to new composite FKs.
        foreach(var table in new[]{"Patient","House"})
            if(c.ExecuteScalar<int>($"SELECT COUNT(*) FROM pragma_table_xinfo('{table}') WHERE name='RelationalOwnerId'")==0)
                c.Execute($"ALTER TABLE {table} ADD COLUMN RelationalOwnerId INTEGER GENERATED ALWAYS AS (COALESCE(UserId,0)) VIRTUAL");
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_Relational_PatientOwner ON Patient(Id,RelationalOwnerId)");
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_Relational_HouseOwner ON House(CasaId,RelationalOwnerId)");
        c.Execute("CREATE INDEX IF NOT EXISTS IX_Patient_User_HouseFamily ON Patient(UserId,HouseId,FamilyId)");
        c.Execute("CREATE INDEX IF NOT EXISTS IX_Visits_User_PatientDate ON Visits(UserId,PatientId,VisitDate)");
        // Removing an index preserves all rows and permits distinct historical applications.
        c.Execute("DROP INDEX IF EXISTS UX_PatientVaccineDose_User_Patient_Dose");
    }
}
