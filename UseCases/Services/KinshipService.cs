using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;
using ACS_View.Infrastructure.Data.Migrations;
namespace ACS_View.UseCases.Services;

internal sealed class KinshipService(IDatabaseService database, ICurrentUserContext context) : IKinshipService
{
    private static KinshipSnapshot Load(SQLiteConnection c,int user) => new(user,
        c.Table<Patient>().Where(p=>p.UserId==user).ToList().ToDictionary(p=>p.Id),
        c.Table<FamilyRelationship>().Where(r=>r.UserId==user).ToList());
    public async Task<KinshipSnapshot> LoadAsync()
    {
        var user=context.RequireCurrentUserId();await database.InitializeAsync();
        KinshipSnapshot? result=null;
        await database.Connection.RunInTransactionAsync(c=>result=Load(c,user));
        if(context.CurrentUserId!=user)throw new InvalidOperationException("A sessão mudou. Reabra a consulta.");
        return result!;
    }
    public async Task<List<Patient>> SearchAsync(string text,int excludeId,CancellationToken cancellationToken)
    {
        var user=context.RequireCurrentUserId();await database.InitializeAsync();cancellationToken.ThrowIfCancellationRequested();
        var pattern="%"+SearchTextNormalizer.Normalize(text).Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_")+"%";
        var result=await database.Connection.QueryAsync<Patient>("SELECT * FROM Patient WHERE UserId=? AND Id<>? AND (SearchName LIKE ? ESCAPE '\\' OR Name LIKE ? ESCAPE '\\') ORDER BY Name LIMIT 50",user,excludeId,pattern,pattern);
        cancellationToken.ThrowIfCancellationRequested();if(context.CurrentUserId!=user)throw new InvalidOperationException("A sessão mudou.");return result;
    }
    private static KinshipSnapshot Apply(KinshipSnapshot before,IReadOnlyList<FamilyRelationship> changes,IReadOnlyList<int> removed)
    {
        var links=before.Relationships.Select(x=>x.Copy()).ToList();
        foreach(var id in removed)
        {
            if(!links.Any(r=>r.Id==id))throw new InvalidOperationException("Vínculo não pertence ao perfil atual.");links.RemoveAll(r=>r.Id==id);
        }
        foreach(var input in changes)
        {
            var r=input.Copy();r.UserId=before.UserId;KinshipEngine.Normalize(r);
            if(r.Id>0)
            {
                var old=links.FirstOrDefault(x=>x.Id==r.Id)??throw new InvalidOperationException("Vínculo não pertence ao perfil atual.");
                if(old.Revision!=r.Revision)throw new InvalidOperationException("O vínculo foi alterado. Recarregue antes de salvar.");
                links.Remove(old);
            }
            links.Add(r);
        }
        var after=before with{Relationships=links};KinshipEngine.Validate(after);return after;
    }
    public async Task<KinshipPreview> PreviewAsync(IReadOnlyList<FamilyRelationship> changes,IReadOnlyList<int> removed)
    {
        if(changes.Count==0&&removed.Count==0)return new KinshipPreview([],[],[]);
        var before=await LoadAsync();return await Task.Run(()=>
        {
            var after=Apply(before,changes,removed);
            return KinshipEngine.Preview(before,after,changes.FirstOrDefault()??before.Relationships.First(r=>removed.Contains(r.Id)));
        });
    }
    public async Task SaveRegistrationAsync(Patient patient,IReadOnlyList<FamilyRelationship> changes,IReadOnlyList<int> removed)
    {
        var user=context.RequireCurrentUserId();await database.InitializeAsync();
        patient.Name=PatientNameRules.NormalizeRequired(patient.Name,"Nome");
        patient.MotherName=PatientNameRules.NormalizeOptional(patient.MotherName,"Nome da mãe");
        patient.FatherName=PatientNameRules.NormalizeOptional(patient.FatherName,"Nome do pai");
        patient.SearchName=SearchTextNormalizer.Normalize(patient.Name);
        patient.SearchMotherName=SearchTextNormalizer.Normalize(patient.MotherName);
        patient.SearchFatherName=SearchTextNormalizer.Normalize(patient.FatherName);
        int originalId=patient.Id,revision=patient.RowRevision;
        try
        {
            await database.Connection.RunInTransactionAsync(c=>
            {
                if(context.CurrentUserId!=user)throw new InvalidOperationException("A sessão mudou.");
                var old=patient.Id>0?c.Table<Patient>().FirstOrDefault(p=>p.Id==patient.Id&&p.UserId==user):null;
                PatientPersistence.Save(c,patient,user,false);
                var mapped=changes.Select(x=>{var r=x.Copy();if(r.FromPatientId==0)r.FromPatientId=patient.Id;if(r.ToPatientId==0)r.ToPatientId=patient.Id;return r;}).ToArray();
                Write(c,user,mapped,removed);
                KinshipSchema.SyncLegacyParents(c,patient,old);
                if(context.CurrentUserId!=user)throw new InvalidOperationException("A sessão mudou.");
                patient.RowRevision=c.ExecuteScalar<int>("SELECT RowRevision FROM Patient WHERE Id=? AND UserId=?",patient.Id,user);
            });
        }
        catch{patient.Id=originalId;patient.RowRevision=revision;throw;}
        DataChangeTracker.MarkPatientsChanged();
    }
    public async Task SaveAsync(IReadOnlyList<FamilyRelationship> changes,IReadOnlyList<int> removed)
    {
        var user=context.RequireCurrentUserId();await database.InitializeAsync();
        await database.Connection.RunInTransactionAsync(c=>
        {
            if(context.CurrentUserId!=user)throw new InvalidOperationException("A sessão mudou.");
            Write(c,user,changes,removed);
            if(context.CurrentUserId!=user)throw new InvalidOperationException("A sessão mudou.");
        });
        DataChangeTracker.MarkPatientsChanged();
    }
    private static void Write(SQLiteConnection c,int user,IReadOnlyList<FamilyRelationship> changes,IReadOnlyList<int> removed)
    {
            var before=Load(c,user);var after=Apply(before,changes,removed);
            foreach(var id in removed)c.Execute("DELETE FROM FamilyRelationship WHERE Id=? AND UserId=?",id,user);
            foreach(var input in changes)
            {
                var r=input.Copy();r.UserId=user;KinshipEngine.Normalize(r);r.UpdatedAt=DateTime.UtcNow;
                if(r.Id==0){r.CreatedAt=r.UpdatedAt;c.Insert(r);}else{r.CreatedAt=before.Relationships.Single(x=>x.Id==r.Id).CreatedAt;r.Revision++;c.Update(r);}
            }
            // Existing name-only fields remain evidence; clear only ID links explicitly removed.
            foreach(var r in before.Relationships.Where(r=>r.TypeCode=="parent" && !after.Relationships.Any(a=>a.TypeCode=="parent"&&a.IsActive&&a.FromPatientId==r.FromPatientId&&a.ToPatientId==r.ToPatientId)))
            {
                c.Execute("UPDATE Patient SET FatherPatientId=NULL WHERE Id=? AND UserId=? AND FatherPatientId=?",r.ToPatientId,user,r.FromPatientId);
                c.Execute("UPDATE Patient SET MotherPatientId=NULL WHERE Id=? AND UserId=? AND MotherPatientId=?",r.ToPatientId,user,r.FromPatientId);
            }
    }

}
