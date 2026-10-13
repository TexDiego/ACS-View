using System.Text.RegularExpressions;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.Infrastructure.Data.Migrations;

internal static class RelationalStore
{
    public static void CreateSchema(SQLiteConnection c)
    {
        c.Execute("""
            CREATE TABLE IF NOT EXISTS FamilyRecord (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, HouseId INTEGER,
                ResponsiblePatientId INTEGER, LocalNumber INTEGER, CreatedAt BIGINT,
                UNIQUE(Id,UserId),
                FOREIGN KEY(HouseId,UserId) REFERENCES House(CasaId,RelationalOwnerId) ON DELETE RESTRICT,
                FOREIGN KEY(ResponsiblePatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("""
            CREATE TABLE IF NOT EXISTS LegacyFamilyMap (
                UserId INTEGER NOT NULL, LegacyHouseId INTEGER NOT NULL, LocalNumber INTEGER NOT NULL,
                SourcePatientId INTEGER NOT NULL DEFAULT 0, FamilyId INTEGER NOT NULL,
                PRIMARY KEY(UserId,LegacyHouseId,LocalNumber,SourcePatientId),
                FOREIGN KEY(FamilyId,UserId) REFERENCES FamilyRecord(Id,UserId) ON DELETE RESTRICT)
            """);
        c.Execute("""
            CREATE TABLE IF NOT EXISTS FamilyMember (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, FamilyId INTEGER NOT NULL,
                PatientId INTEGER NOT NULL, JoinedAt BIGINT, LeftAt BIGINT, CreatedAt BIGINT NOT NULL, UpdatedAt BIGINT,
                FOREIGN KEY(FamilyId,UserId) REFERENCES FamilyRecord(Id,UserId) ON DELETE RESTRICT,
                FOREIGN KEY(PatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_FamilyMember_Active ON FamilyMember(PatientId) WHERE LeftAt IS NULL");
        c.Execute("""
            CREATE TABLE IF NOT EXISTS FamilyResidenceHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, FamilyId INTEGER NOT NULL,
                HouseId INTEGER NOT NULL, StartedAt BIGINT, EndedAt BIGINT,
                FOREIGN KEY(FamilyId,UserId) REFERENCES FamilyRecord(Id,UserId) ON DELETE RESTRICT,
                FOREIGN KEY(HouseId,UserId) REFERENCES House(CasaId,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_FamilyResidence_Active ON FamilyResidenceHistory(FamilyId) WHERE EndedAt IS NULL");
        c.Execute("""
            CREATE TABLE IF NOT EXISTS PatientSusNumber (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, PatientId INTEGER NOT NULL,
                Number TEXT NOT NULL CHECK(length(Number)>0), IsPrimary INTEGER NOT NULL CHECK(IsPrimary IN(0,1)), OriginalOrder INTEGER NOT NULL,
                UNIQUE(PatientId,Number), FOREIGN KEY(PatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_PatientSus_Primary ON PatientSusNumber(PatientId) WHERE IsPrimary=1");
        c.Execute("CREATE INDEX IF NOT EXISTS IX_PatientSus_UserNumber ON PatientSusNumber(UserId,Number)");
        c.Execute("""
            CREATE TABLE IF NOT EXISTS PatientPhone (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, PatientId INTEGER NOT NULL,
                Number TEXT NOT NULL CHECK(length(trim(Number))>0), NormalizedNumber TEXT CHECK(NormalizedNumber IS NULL OR (length(NormalizedNumber) BETWEEN 8 AND 15 AND NormalizedNumber NOT GLOB '*[^0-9]*')),
                Type TEXT NOT NULL, ContactName TEXT, Relationship TEXT, IsPrimary INTEGER NOT NULL CHECK(IsPrimary IN(0,1)),
                HasWhatsApp INTEGER NOT NULL, Notes TEXT,
                UNIQUE(PatientId,NormalizedNumber),
                FOREIGN KEY(PatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_PatientPhone_Primary ON PatientPhone(PatientId) WHERE IsPrimary=1");
        c.Execute("""
            CREATE TABLE IF NOT EXISTS TerritorialAssignment (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, HouseId INTEGER NOT NULL,
                CensusSector TEXT NOT NULL, BlockNumber TEXT NOT NULL, AreaCode TEXT NOT NULL, SectorCode TEXT NOT NULL,
                ValidFrom BIGINT, ValidTo BIGINT,
                FOREIGN KEY(HouseId,UserId) REFERENCES House(CasaId,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_Territorial_Active ON TerritorialAssignment(HouseId) WHERE ValidTo IS NULL");
        c.CreateTable<MigrationIssue>();
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_MigrationIssue_Source ON MigrationIssue(UserId,SourceTable,SourceId,Code,Evidence)");
        c.Execute("CREATE TABLE IF NOT EXISTS HealthConditionDefinition (Id INTEGER PRIMARY KEY, Code TEXT NOT NULL UNIQUE, Description TEXT NOT NULL)");
        if (!c.GetTableInfo("PatientConditions").Any(x=>x.Name=="ConditionId"))
            c.Execute("ALTER TABLE PatientConditions ADD COLUMN ConditionId INTEGER NULL");
        // No tables are rebuilt. Guards validate new references while retaining old orphan rows.
        foreach (var table in new[] { "Patient", "House", "Family", "PatientConditions", "PatientCID", "PatientVaccineDose", "PatientPregnancy", "PatientInsulinDependency", "PatientBolsaFamilia", "Visits", "VisitBatch", "VisitCareLine", "CareNotification", "Note", "ImportHistory", "FamilyRecord", "FamilyMember", "FamilyResidenceHistory", "PatientPhone", "PatientSusNumber", "TerritorialAssignment", "MigrationIssue" })
        {
            c.Execute($"CREATE TRIGGER IF NOT EXISTS {table}_OwnerImmutable BEFORE UPDATE OF UserId ON {table} WHEN NEW.UserId IS NOT OLD.UserId BEGIN SELECT RAISE(ABORT,'Owner cannot change'); END");
        }
        foreach (var table in new[] { "PatientConditions", "PatientCID", "PatientVaccineDose", "PatientPregnancy", "PatientInsulinDependency", "PatientBolsaFamilia", "Visits", "CareNotification" })
            GuardReference(c, table, "PatientId", "Patient", "Id");
        GuardReference(c, "Patient", "HouseId", "House", "CasaId");
        foreach (var field in new[] { "MotherPatientId", "FatherPatientId", "FamilyResponsiblePatientId" })
            GuardReference(c, "Patient", field, "Patient", "Id");
        GuardReference(c, "PatientBolsaFamilia", "ResponsiblePatientId", "Patient", "Id");
        GuardReference(c, "Visits", "HouseId", "House", "CasaId");
        GuardReference(c, "Visits", "BatchId", "VisitBatch", "Id");
        GuardReference(c, "VisitBatch", "HouseId", "House", "CasaId");
        GuardReference(c, "VisitCareLine", "VisitId", "Visits", "Id");
        c.Execute("CREATE TRIGGER IF NOT EXISTS PatientCID_Catalog_Insert BEFORE INSERT ON PatientCID WHEN NOT EXISTS(SELECT 1 FROM CidSubcategory WHERE Id=NEW.CidId) BEGIN SELECT RAISE(ABORT,'Unknown CID'); END");
        c.Execute("CREATE TRIGGER IF NOT EXISTS PatientCID_Catalog_Update BEFORE UPDATE OF CidId ON PatientCID WHEN NEW.CidId IS NOT OLD.CidId AND NOT EXISTS(SELECT 1 FROM CidSubcategory WHERE Id=NEW.CidId) BEGIN SELECT RAISE(ABORT,'Unknown CID'); END");
        c.Execute("CREATE TRIGGER IF NOT EXISTS Patient_Revision AFTER UPDATE ON Patient WHEN NEW.RowRevision=OLD.RowRevision BEGIN UPDATE Patient SET RowRevision=OLD.RowRevision+1 WHERE Id=NEW.Id; END");
        c.Execute("CREATE TRIGGER IF NOT EXISTS House_Revision AFTER UPDATE ON House WHEN NEW.RowRevision=OLD.RowRevision BEGIN UPDATE House SET RowRevision=OLD.RowRevision+1 WHERE CasaId=NEW.CasaId; END");
        // Clinical history has no delete cascade. Individual Patient deletion is rejected;
        // the repository archives it. Explicit account cleanup deletes dependents first.
        foreach (var table in new[] { "PatientConditions", "PatientCID", "PatientVaccineDose", "PatientPregnancy", "PatientInsulinDependency", "PatientBolsaFamilia", "Visits" })
            c.Execute($"CREATE TRIGGER IF NOT EXISTS Patient_Keep_{table} BEFORE DELETE ON Patient WHEN EXISTS(SELECT 1 FROM {table} WHERE PatientId=OLD.Id AND UserId=OLD.UserId) BEGIN SELECT RAISE(ABORT,'Patient has history; archive instead'); END");
    }

    private static void GuardReference(SQLiteConnection c, string table, string field, string parent, string key)
    {
        var invalid = $"NEW.{field}>0 AND NOT EXISTS(SELECT 1 FROM {parent} WHERE {key}=NEW.{field} AND UserId=NEW.UserId)";
        c.Execute($"CREATE TRIGGER IF NOT EXISTS {table}_{field}_Insert BEFORE INSERT ON {table} WHEN {invalid} BEGIN SELECT RAISE(ABORT,'Invalid owner reference'); END");
        c.Execute($"CREATE TRIGGER IF NOT EXISTS {table}_{field}_Update BEFORE UPDATE OF {field} ON {table} WHEN NEW.{field} IS NOT OLD.{field} AND {invalid} BEGIN SELECT RAISE(ABORT,'Invalid owner reference'); END");
    }

    public static void MigrateData(SQLiteConnection c,bool preserveExistingConditionIdentity=false)
    {
        foreach (var patient in c.Table<Patient>().ToList())
        {
            SyncPatient(c, patient, true);
            if (patient.UserId == 0) Issue(c, 0, "Patient", patient.Id, "UnownedLegacy", "");
        }
        c.Execute("INSERT OR IGNORE INTO MigrationIssue(UserId,SourceTable,SourceId,Code,Evidence,CreatedAt) SELECT COALESCE(UserId,0),'Patient',Id,'UnknownBirthDate','',? FROM Patient WHERE BirthDate IS NULL OR BirthDate=0",DateTime.UtcNow);
        foreach (var family in c.Table<Family>())
            Issue(c, family.UserId, "Family", family.IdFamilia, "LegacyFamilyUnresolved", $"{family.IdFamilia}:{family.IdPessoa}");
        foreach (var group in c.Table<PatientVaccineDose>().ToList().GroupBy(x => (x.UserId, x.PatientId, x.DoseKey)).Where(g => g.Count()>1))
            foreach (var dose in group) Issue(c, dose.UserId, "PatientVaccineDose", dose.Id, "MultipleApplications", "");
        // Migrate boolean vaccine evidence without manufacturing an application date.
        foreach (var legacy in c.Table<Vaccines>())
        {
            if(c.Table<Patient>().FirstOrDefault(p=>p.Id==legacy.Id && p.UserId==legacy.UserId)==null)
                Issue(c,legacy.UserId,"Vaccines",legacy.Id,"MissingPatientForLegacyDoses","");
            foreach (var column in VaccineDoseCatalog.LegacyBooleanColumns)
                if (typeof(Vaccines).GetProperty(column.Key)?.GetValue(legacy) is true)
                    c.Execute("""
                        INSERT INTO PatientVaccineDose(UserId,PatientId,DoseKey,IsApplied,AppliedAt,LotNumber,Notes,CreatedAt)
                        SELECT ?,?,?,1,NULL,'','',NULL WHERE NOT EXISTS(
                            SELECT 1 FROM PatientVaccineDose WHERE UserId=? AND PatientId=? AND DoseKey=?)
                        AND EXISTS(SELECT 1 FROM Patient WHERE UserId=? AND Id=?)
                        """, legacy.UserId, legacy.Id, column.Value, legacy.UserId, legacy.Id, column.Value, legacy.UserId, legacy.Id);
        }
        if (c.GetTableInfo("Patient").Any(x => x.Name=="BolsaFamilia"))
            c.Execute("""
                INSERT INTO PatientBolsaFamilia(UserId,PatientId,ResponsiblePatientId,NisNumber)
                SELECT UserId,Id,Id,'' FROM Patient p WHERE BolsaFamilia=1 AND NOT EXISTS(
                    SELECT 1 FROM PatientBolsaFamilia b WHERE b.UserId=p.UserId AND b.PatientId=p.Id)
                """);
        c.Execute("""
            INSERT INTO PatientInsulinDependency(UserId,PatientId)
            SELECT DISTINCT p.UserId,p.Id FROM Patient p JOIN PatientConditions pc ON pc.PatientId=p.Id AND pc.UserId=p.UserId
            WHERE pc.Description='Insulinodependente' COLLATE NOCASE AND NOT EXISTS(
                SELECT 1 FROM PatientInsulinDependency d WHERE d.PatientId=p.Id AND d.UserId=p.UserId)
            """);
        c.Execute("""
            INSERT INTO PatientPregnancy(UserId,PatientId,LastMenstrualPeriod,ExpectedBirthDate,Status,ManualRisk,InformedChildrenCount,CreatedAt,EndedAt,EndType,Notes)
            SELECT DISTINCT p.UserId,p.Id,NULL,NULL,1,0,NULL,?,NULL,NULL,'' FROM Patient p
            JOIN PatientConditions pc ON pc.PatientId=p.Id AND pc.UserId=p.UserId
            WHERE pc.Description='Gestante' COLLATE NOCASE AND NOT EXISTS(
                SELECT 1 FROM PatientPregnancy g WHERE g.PatientId=p.Id AND g.UserId=p.UserId)
            """,DateTime.UtcNow);
        // CreatedAt is migration bookkeeping, never gestation onset or last menstrual period.
        // Fixed IDs do not depend on seeding order or on a translated display name.
        var descriptions = HealthConditionCatalog.Conditions.Concat([HealthConditionCatalog.Insulinodependente]).ToArray();
        var codes = new[] { "pregnancy", "diabetes", "hypertension", "tuberculosis", "leprosy", "bedridden", "homebound", "mental", "smoking", "alcohol", "disability", "cancer", "bolsa-familia", "drug-dependency", "immunodeficiency", "insulin" };
        for (var i=0;i<descriptions.Length;i++)
        {
            c.Execute("INSERT OR IGNORE INTO HealthConditionDefinition VALUES(?,?,?)", i+1,codes[i],descriptions[i]);
            if(!preserveExistingConditionIdentity)c.Execute("UPDATE PatientConditions SET ConditionId=? WHERE Description=? COLLATE NOCASE",i+1,descriptions[i]);
        }
        if(!preserveExistingConditionIdentity)c.Execute("UPDATE PatientConditions SET ConditionId=2 WHERE Description LIKE 'Diabetes%' AND ConditionId IS NULL");
        foreach(var condition in c.Table<PatientConditions>().ToList())
        {
            if(preserveExistingConditionIdentity)
            {
                if(condition.ConditionId==null)Issue(c,condition.UserId,"PatientConditions",condition.Id,"PendingConditionIdentity","");
            }
            else SetConditionIdentity(c,condition);
        }
        foreach(var action in new[]{"INSERT","UPDATE OF Description"})
            c.Execute($"CREATE TRIGGER IF NOT EXISTS PatientConditions_Stable_{(action=="INSERT"?"Insert":"Update")} AFTER {action} ON PatientConditions BEGIN UPDATE PatientConditions SET ConditionId=(SELECT Id FROM HealthConditionDefinition WHERE Description=NEW.Description COLLATE NOCASE OR (Code='diabetes' AND NEW.Description LIKE 'Diabetes%') LIMIT 1) WHERE Id=NEW.Id; END");
        // Validate implicit old references, without rewriting or dropping them.
        foreach (var relation in new[] { ("Patient","HouseId","House","CasaId"), ("PatientCID","PatientId","Patient","Id"),
            ("PatientConditions","PatientId","Patient","Id"), ("PatientPregnancy","PatientId","Patient","Id"),
            ("PatientVaccineDose","PatientId","Patient","Id"), ("Visits","PatientId","Patient","Id"),
            ("Visits","HouseId","House","CasaId"), ("VisitCareLine","VisitId","Visits","Id") })
            c.Execute($"""
                INSERT OR IGNORE INTO MigrationIssue(UserId,SourceTable,SourceId,Code,Evidence,CreatedAt)
                SELECT COALESCE(s.UserId,0),?,s.Id,?,'',? FROM {relation.Item1} s
                WHERE s.{relation.Item2}>0 AND NOT EXISTS(SELECT 1 FROM {relation.Item3} p
                    WHERE p.{relation.Item4}=s.{relation.Item2} AND p.UserId=s.UserId)
                """,relation.Item1,"InvalidReference:"+relation.Item2,DateTime.UtcNow);
    }

    public static void Issue(SQLiteConnection c, int user, string table, int id, string code, string evidence) =>
        c.Execute("INSERT OR IGNORE INTO MigrationIssue(UserId,SourceTable,SourceId,Code,Evidence,CreatedAt) VALUES(?,?,?,?,?,?)",user,table,id,code,evidence,DateTime.UtcNow);

    public static void WritePatientFamilyFields(SQLiteConnection c,Patient p) =>
        c.Execute("UPDATE Patient SET HouseId=?,FamilyId=?,FamilyResponsiblePatientId=?,FamilyResponsibleSus=? WHERE Id=? AND UserId=?",
            p.HouseId,p.FamilyId,p.FamilyResponsiblePatientId,p.FamilyResponsibleSus,p.Id,p.UserId);

    public static void SetConditionIdentity(SQLiteConnection c,PatientConditions condition)
    {
        var canonical=HealthConditionCatalog.GetKey(condition.Description ?? "");
        condition.ConditionId=c.ExecuteScalar<int?>("SELECT Id FROM HealthConditionDefinition WHERE Description=?",canonical);
        c.Execute("UPDATE PatientConditions SET ConditionId=? WHERE Id=? AND UserId=?",condition.ConditionId,condition.Id,condition.UserId);
        if(condition.ConditionId==null)Issue(c,condition.UserId,"PatientConditions",condition.Id,"UnmappedCondition",condition.Description ?? "");
    }

    // Called inside the same transaction as every Patient INSERT/UPDATE, including import.
    public static void SyncPatient(SQLiteConnection c, Patient p, bool migrated = false)
    {
        SyncSusNumbers(c, p);
        SyncPatientFamily(c, p, migrated);
    }

    internal static void IssueLegacySus(SQLiteConnection c, Patient p)
    {
        if (!string.IsNullOrWhiteSpace(p.SusNumber))
            Issue(c, p.UserId, "Patient", p.Id, "LegacyCnsEvidence", p.SusNumber);
    }

    internal static void BackfillSusNumbers(SQLiteConnection c)
    {
        foreach (var p in c.Table<Patient>().ToList())
        {
            IssueLegacySus(c, p);
            var old = c.Table<PatientSusNumber>().Where(n => n.PatientId == p.Id).ToList();
            var order = old.Count == 0 ? 0 : old.Max(n => n.OriginalOrder) + 1;
            foreach (var number in SusNumberSet.Parse(p.SusNumber).Where(n => old.All(o => o.Number != n)))
            {
                var row = new PatientSusNumber { PatientId = p.Id, UserId = p.UserId, Number = number,
                    OriginalOrder = order++, IsPrimary = old.Count == 0 };
                c.Insert(row);
                old.Add(row);
            }
        }
    }

    private static void SyncSusNumbers(SQLiteConnection c, Patient p)
    {
        var numbers = p.SusNumbers;
        var tokens = (p.SusNumber ?? "").Split([';', ',', '/', '|', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != numbers.Count || tokens.Any(t => Regex.Replace(t,@"\D", "").Length != 15))
            Issue(c,p.UserId,"Patient",p.Id,"CnsNeedsReview",p.SusNumber ?? "");
        var old = c.Table<PatientSusNumber>().Where(x => x.PatientId==p.Id && x.UserId==p.UserId).ToList();
        var requested = p.SusNumberRecords;
        if (requested?.Any(n => n.Id != 0 && old.All(o => o.Id != n.Id)) == true)
            throw new InvalidOperationException("SUS não pertence ao paciente atual.");
        foreach (var number in old.Where(x => requested is null ? !numbers.Contains(x.Number) : requested.All(n => n.Id != x.Id && !(n.Id == 0 && n.Number == x.Number)))) c.Delete(number);
        c.Execute("UPDATE PatientSusNumber SET IsPrimary=0, Number='__pending_sus_' || Id WHERE PatientId=? AND UserId=?",p.Id,p.UserId);
        for (var i=0;i<numbers.Count;i++)
        {
            var record = requested?.FirstOrDefault(n => n.Number == numbers[i]);
            var item=(record is null || record.Id == 0 ? old.FirstOrDefault(x => x.Number==numbers[i]) : old.FirstOrDefault(x => x.Id == record.Id))
                ?? new PatientSusNumber { PatientId=p.Id,UserId=p.UserId };
            item.Number = numbers[i];
            item.IsPrimary=i==0; item.OriginalOrder=i;
            if(item.Id==0)c.Insert(item);else c.Update(item);
            var conflicts=c.Table<PatientSusNumber>().Where(x => x.UserId==p.UserId && x.Number==item.Number && x.PatientId!=p.Id).ToList();
            foreach(var conflict in conflicts)
            {
                Issue(c,p.UserId,"Patient",p.Id,"CnsShared",item.Number);
                Issue(c,p.UserId,"Patient",conflict.PatientId,"CnsShared",item.Number);
            }
        }
    }

    private static void SyncPatientFamily(SQLiteConnection c, Patient p, bool migrated)
    {
        var active=c.Table<FamilyMember>().FirstOrDefault(x=>x.PatientId==p.Id && x.LeftAt==null);
        int? familyId=null;
        if(p.FamilyId>0)
        {
            var validHouse=c.Table<House>().FirstOrDefault(h=>h.CasaId==p.HouseId && h.UserId==p.UserId);
            var sourcePatient=validHouse==null ? p.Id : 0;
            if(validHouse==null) Issue(c,p.UserId,"Patient",p.Id,"FamilyWithoutValidHouse",$"{p.HouseId}:{p.FamilyId}");
            var mapping=c.Query<MapRow>("SELECT FamilyId FROM LegacyFamilyMap WHERE UserId=? AND LegacyHouseId=? AND LocalNumber=? AND SourcePatientId=?",p.UserId,p.HouseId,p.FamilyId,sourcePatient).SingleOrDefault();
            if(mapping==null)
            {
                var family=new FamilyRecord { UserId=p.UserId,HouseId=validHouse?.CasaId,LocalNumber=p.FamilyId,CreatedAt=migrated?null:DateTime.UtcNow };
                c.Insert(family);
                c.Execute("INSERT INTO LegacyFamilyMap VALUES(?,?,?,?,?)",p.UserId,p.HouseId,p.FamilyId,sourcePatient,family.Id);
                if(validHouse!=null)c.Insert(new FamilyResidenceHistory { UserId=p.UserId,FamilyId=family.Id,HouseId=validHouse.CasaId,StartedAt=migrated?null:DateTime.UtcNow });
                familyId=family.Id;
            }
            else
            {
                var mapped=c.Find<FamilyRecord>(mapping.FamilyId);
                if(!migrated && mapped.HouseId!=validHouse?.CasaId)
                    throw new InvalidOperationException("Identificador familiar histórico já transferido; use um novo número local.");
                familyId=mapping.FamilyId;
            }
        }
        if(active!=null && active.FamilyId!=familyId)
        {
            if(migrated)
            {
                Issue(c,p.UserId,"Patient",p.Id,"PreexistingMembershipConflict",$"{active.FamilyId}:{familyId}");
                return;
            }
            active.LeftAt=DateTime.UtcNow; active.UpdatedAt=DateTime.UtcNow; c.Update(active);
        }
        if(familyId.HasValue && (active==null || active.FamilyId!=familyId))
            c.Insert(new FamilyMember { UserId=p.UserId,FamilyId=familyId.Value,PatientId=p.Id,JoinedAt=migrated?null:DateTime.UtcNow,CreatedAt=DateTime.UtcNow });
        if(familyId.HasValue) UpdateResponsible(c,familyId.Value);
        if(active!=null && active.FamilyId!=familyId) UpdateResponsible(c,active.FamilyId);
    }

    public static void UpdateResponsible(SQLiteConnection c, int familyId)
    {
        var members=c.Query<Patient>("SELECT p.* FROM Patient p JOIN FamilyMember m ON m.PatientId=p.Id AND m.UserId=p.UserId WHERE m.FamilyId=? AND m.LeftAt IS NULL",familyId);
        var explicitIds=members.Where(p=>p.FamilyResponsiblePatientId>0).Select(p=>p.FamilyResponsiblePatientId!.Value).Distinct().ToArray();
        var responsible=explicitIds.Length==1 && members.Any(p=>p.Id==explicitIds[0]) ? (int?)explicitIds[0] : null;
        if(explicitIds.Length==0)
        {
            var reported=members.Where(p=>!string.IsNullOrWhiteSpace(p.FamilyResponsibleSus)).Select(p=>p.FamilyResponsibleSus).ToArray();
            var candidates=members.Where(p=>reported.Length>0 && reported.All(p.HasSusNumber)).ToList();
            if(candidates.Count==1)responsible=candidates[0].Id;
            else if(reported.Length>0) Issue(c,members[0].UserId,"FamilyRecord",familyId,"ResponsibleSusUnresolved","");
        }
        var family=c.Find<FamilyRecord>(familyId);
        if(explicitIds.Length>0 && responsible==null) Issue(c,family.UserId,"FamilyRecord",familyId,"ResponsibleConflict","");
        family.ResponsiblePatientId=responsible; c.Update(family);
    }
    private sealed class MapRow { public int FamilyId { get; set; } }

    public static void ValidateProjection(SQLiteConnection c)
    {
        foreach(var table in new[]{"FamilyRecord","FamilyMember","FamilyResidenceHistory","LegacyFamilyMap","PatientPhone","PatientSusNumber","TerritorialAssignment","MigrationIssue","HealthConditionDefinition"})
            if(c.GetTableInfo(table).Count==0)throw new InvalidOperationException("Esquema relacional incompleto.");
        var susByPatient=c.Table<PatientSusNumber>().ToList().GroupBy(x=>x.PatientId).ToDictionary(g=>g.Key,g=>g.OrderBy(x=>x.OriginalOrder).ToList());
        var membersByPatient=c.Table<FamilyMember>().Where(x=>x.LeftAt==null).ToList().GroupBy(x=>x.PatientId).ToDictionary(g=>g.Key,g=>g.ToList());
        foreach(var p in c.Table<Patient>())
        {
            var projected=susByPatient.GetValueOrDefault(p.Id) ?? [];
            if(SusNumberSet.Parse(p.SusNumber).Except(projected.Select(x=>x.Number)).Any() || projected.Any(x=>x.UserId!=p.UserId))
                throw new InvalidOperationException("Projeção CNS divergente; conciliação necessária.");
            var membership=membersByPatient.GetValueOrDefault(p.Id) ?? [];
            if(membership.Count != (p.FamilyId>0 ? 1:0))
                throw new InvalidOperationException("Projeção familiar divergente; conciliação necessária.");
        }
    }
}
