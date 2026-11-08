using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;
namespace ACS_View.Infrastructure.Data.Migrations;

internal static class KinshipSchema
{
    internal static void Create(SQLiteConnection c)
    {
        c.CreateTable<FamilyRelationshipType>();
        foreach(var t in KinshipCatalog.Types)
            c.Execute("INSERT OR IGNORE INTO FamilyRelationshipType(Code,Category,ForwardLabel,ReverseLabel,Symmetric,Generation) VALUES(?,?,?,?,?,?)",t.Code,t.Category,t.Forward,t.Reverse,t.Symmetric,t.Generation);
        c.Execute("""
            CREATE TABLE IF NOT EXISTS FamilyRelationship (
              Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL CHECK(UserId>0),
              FromPatientId INTEGER NOT NULL, ToPatientId INTEGER NOT NULL,
              TypeCode TEXT NOT NULL REFERENCES FamilyRelationshipType(Code),
              ForwardLabel TEXT NOT NULL, ReverseLabel TEXT NOT NULL,
              Nature TEXT NOT NULL CHECK(Nature IN ('Biológico','Adotivo','Socioafetivo','Não informado')),
              IsActive INTEGER NOT NULL CHECK(IsActive IN (0,1)), StartDate BIGINT, EndDate BIGINT,
              EndReason TEXT NOT NULL, CreatedAt BIGINT NOT NULL, UpdatedAt BIGINT NOT NULL, Revision INTEGER NOT NULL DEFAULT 0,
              CHECK(FromPatientId<>ToPatientId), CHECK(StartDate IS NULL OR EndDate IS NULL OR EndDate>=StartDate),
              CHECK(IsActive=0 OR EndDate IS NULL),
              FOREIGN KEY(FromPatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT,
              FOREIGN KEY(ToPatientId,UserId) REFERENCES Patient(Id,RelationalOwnerId) ON DELETE RESTRICT)
            """);
        c.Execute("CREATE INDEX IF NOT EXISTS IX_Kinship_From ON FamilyRelationship(UserId,FromPatientId,IsActive)");
        c.Execute("CREATE INDEX IF NOT EXISTS IX_Kinship_To ON FamilyRelationship(UserId,ToPatientId,IsActive)");
        c.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_Kinship_Equivalent ON FamilyRelationship(UserId,FromPatientId,ToPatientId,TypeCode,CASE WHEN TypeCode='custom' THEN ForwardLabel ELSE '' END,CASE WHEN TypeCode='custom' THEN ReverseLabel ELSE '' END,CASE WHEN TypeCode IN ('marriage','union','partner') THEN COALESCE(StartDate,-1) ELSE -1 END,CASE WHEN TypeCode IN ('marriage','union','partner') THEN COALESCE(EndDate,-1) ELSE -1 END,CASE WHEN TypeCode IN ('marriage','union','partner') THEN IsActive ELSE 1 END)");
        c.Execute("CREATE TRIGGER IF NOT EXISTS FamilyRelationship_OwnerImmutable BEFORE UPDATE OF UserId ON FamilyRelationship WHEN NEW.UserId<>OLD.UserId BEGIN SELECT RAISE(ABORT,'Owner cannot change'); END");
        foreach(var operation in new[]{"INSERT","UPDATE"})
            c.Execute($"CREATE TRIGGER IF NOT EXISTS FamilyRelationship_Normalize_{operation} BEFORE {operation} ON FamilyRelationship WHEN NEW.FromPatientId>NEW.ToPatientId AND (NEW.TypeCode='custom' OR EXISTS(SELECT 1 FROM FamilyRelationshipType WHERE Code=NEW.TypeCode AND Symmetric=1)) BEGIN SELECT RAISE(ABORT,'Inverted symmetric relationship'); END");
        BackfillLegacyParents(c);
    }
    internal static void BackfillLegacyParents(SQLiteConnection c,int? userId=null)
    {
        var patients=c.Table<Patient>().Where(p=>p.UserId>0).ToList();
        var existing=c.Table<FamilyRelationship>().ToList().ToLookup(r=>r.UserId);
        foreach(var group in patients.Where(p=>!userId.HasValue||p.UserId==userId.Value).GroupBy(p=>p.UserId))
        {
            var people=group.ToDictionary(p=>p.Id);var links=existing[group.Key].ToList();var additions=new List<FamilyRelationship>();
            var keys=links.Where(r=>r.TypeCode=="parent").Select(r=>(r.FromPatientId,r.ToPatientId)).ToHashSet();
            foreach(var child in group)
                foreach(var (id,label) in new[]{(child.FatherPatientId,"Pai"),(child.MotherPatientId,"Mãe")})
                {
                    if(id is not >0)continue;
                    if(id==child.Id||!people.ContainsKey(id.Value)){RelationalStore.Issue(c,child.UserId,"Patient",child.Id,"KinshipLegacyConflict","");continue;}
                    if(!keys.Add((id.Value,child.Id)))continue;
                    additions.Add(new FamilyRelationship{UserId=child.UserId,FromPatientId=id.Value,ToPatientId=child.Id,ForwardLabel=label,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow});
                }
            if(additions.Count==0)continue;
            try{KinshipEngine.Validate(new(group.Key,people,links.Concat(additions).ToArray()));foreach(var r in additions)c.Insert(r);}
            catch(InvalidOperationException)
            {
                // Retain inconsistent legacy IDs for explicit reconciliation; never change old records.
                foreach(var r in additions)
                {
                    try{KinshipEngine.Validate(new(group.Key,people,links.Append(r).ToArray()));c.Insert(r);links.Add(r);}
                    catch(InvalidOperationException){RelationalStore.Issue(c,group.Key,"Patient",r.ToPatientId,"KinshipLegacyConflict","");}
                }
            }
        }
    }
    internal static void SyncLegacyParents(SQLiteConnection c,Patient patient,Patient? old)
    {
        if(c.GetTableInfo("FamilyRelationship").Count==0)return;
        var links=c.Table<FamilyRelationship>().Where(r=>r.UserId==patient.UserId).ToList();
        var changed=false;
        foreach(var (id,previous,label) in new[]{(patient.FatherPatientId,old?.FatherPatientId,"Pai"),(patient.MotherPatientId,old?.MotherPatientId,"Mãe")})
        {
            if(id==previous)continue;
            if(previous is >0 && previous!=patient.FatherPatientId && previous!=patient.MotherPatientId)
                foreach(var r in links.Where(r=>r.TypeCode=="parent"&&r.FromPatientId==previous&&r.ToPatientId==patient.Id&&r.IsActive))
                {r.IsActive=false;r.UpdatedAt=DateTime.UtcNow;r.Revision++;c.Update(r);changed=true;}
            if(id is not >0)continue;
            var existing=links.FirstOrDefault(r=>r.TypeCode=="parent"&&r.FromPatientId==id&&r.ToPatientId==patient.Id);
            if(existing!=null){if(!existing.IsActive){existing.IsActive=true;existing.UpdatedAt=DateTime.UtcNow;existing.Revision++;c.Update(existing);changed=true;}continue;}
            var relationship=new FamilyRelationship{UserId=patient.UserId,FromPatientId=id.Value,ToPatientId=patient.Id,ForwardLabel=label,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
            links.Add(relationship);c.Insert(relationship);changed=true;
        }
        if(changed)KinshipEngine.Validate(new(patient.UserId,c.Table<Patient>().Where(p=>p.UserId==patient.UserId).ToList().ToDictionary(p=>p.Id),links));
    }

}
