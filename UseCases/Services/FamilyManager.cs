using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.Migrations;
using SQLite;

namespace ACS_View.UseCases.Services;

internal sealed class FamilyManager(IDatabaseService database, ICurrentUserContext context) : IFamilyManager
{
    public async Task AddPeopleToFamily(List<int> ids, int houseId, int familyId, int responsiblePatientId)
    {
        var user=context.RequireCurrentUserId();
        await database.Connection.RunInTransactionAsync(c=>
        {
            if(familyId<=0 || !ids.Contains(responsiblePatientId) || c.Table<House>().FirstOrDefault(h=>h.CasaId==houseId && h.UserId==user && h.IsActive)==null)
                throw new InvalidOperationException("Família, responsável ou residência inválidos.");
            var people=ids.Distinct().Select(id=>c.Table<Patient>().FirstOrDefault(p=>p.Id==id && p.UserId==user)
                ?? throw new InvalidOperationException("Paciente não pertence à conta atual.")).ToList();
            var responsible=people.Single(p=>p.Id==responsiblePatientId);
            foreach(var person in people)
            {
                person.HouseId=houseId; person.FamilyId=familyId;
                person.FamilyResponsiblePatientId=responsiblePatientId; person.FamilyResponsibleSus=responsible.PrimarySusNumber;
                RelationalStore.WritePatientFamilyFields(c,person); RelationalStore.SyncPatient(c,person);
            }
            // Resolve after all members have arrived; intermediate responsibility is never committed.
            var family=c.Query<FamilyRecord>("SELECT f.* FROM FamilyRecord f JOIN LegacyFamilyMap m ON m.FamilyId=f.Id AND m.UserId=f.UserId WHERE m.UserId=? AND m.LegacyHouseId=? AND m.LocalNumber=? AND m.SourcePatientId=0",user,houseId,familyId).Single();
            RelationalStore.UpdateResponsible(c,family.Id);
        });
        DataChangeTracker.MarkPatientsChanged();
    }
    public async Task RemovePersonFromFamily(int id)
    {
        var user=context.RequireCurrentUserId();
        await database.Connection.RunInTransactionAsync(c=>
        {
            var person=c.Table<Patient>().FirstOrDefault(p=>p.Id==id && p.UserId==user)
                ?? throw new InvalidOperationException("Paciente não pertence à conta atual.");
            Detach(c,person);
        });
        DataChangeTracker.MarkPatientsChanged();
    }
    public async Task RemoveFamilyAsync(int houseId,int localNumber)
    {
        var user=context.RequireCurrentUserId();
        await database.Connection.RunInTransactionAsync(c=>
        {
            foreach(var person in c.Table<Patient>().Where(p=>p.UserId==user && p.HouseId==houseId && p.FamilyId==localNumber).ToList()) Detach(c,person);
        });
        DataChangeTracker.MarkPatientsChanged();
    }
    private static void Detach(SQLiteConnection c,Patient p)
    {
        p.HouseId=-1; p.FamilyId=-1; p.FamilyResponsiblePatientId=null; p.FamilyResponsibleSus=null;
        RelationalStore.WritePatientFamilyFields(c,p); RelationalStore.SyncPatient(c,p);
    }
    public async Task MoveFamilyAsync(int familyRecordId,int destinationHouseId)
    {
        var user=context.RequireCurrentUserId();
        await database.Connection.RunInTransactionAsync(c=>
        {
            var family=c.Table<FamilyRecord>().FirstOrDefault(f=>f.Id==familyRecordId && f.UserId==user)
                ?? throw new InvalidOperationException("Família não pertence à conta atual.");
            if(c.Table<House>().FirstOrDefault(h=>h.CasaId==destinationHouseId && h.UserId==user && h.IsActive)==null)
                throw new InvalidOperationException("Residência não pertence à conta atual.");
            if(family.HouseId==destinationHouseId)return;
            var number=c.ExecuteScalar<int>("SELECT COALESCE(MAX(LocalNumber),0)+1 FROM LegacyFamilyMap WHERE UserId=? AND LegacyHouseId=?",user,destinationHouseId);
            c.Execute("INSERT INTO LegacyFamilyMap VALUES(?,?,?,?,?)",user,destinationHouseId,number,0,family.Id);
            c.Execute("UPDATE FamilyResidenceHistory SET EndedAt=? WHERE FamilyId=? AND EndedAt IS NULL",DateTime.UtcNow,family.Id);
            c.Insert(new FamilyResidenceHistory { UserId=user,FamilyId=family.Id,HouseId=destinationHouseId,StartedAt=DateTime.UtcNow });
            family.HouseId=destinationHouseId;family.LocalNumber=number;c.Update(family);
            foreach(var person in c.Query<Patient>("SELECT p.* FROM Patient p JOIN FamilyMember m ON m.PatientId=p.Id AND m.UserId=p.UserId WHERE m.FamilyId=? AND m.LeftAt IS NULL",family.Id))
            {
                person.HouseId=destinationHouseId;person.FamilyId=number;RelationalStore.WritePatientFamilyFields(c,person);RelationalStore.SyncPatient(c,person);
            }
        });
        DataChangeTracker.MarkAllChanged();
    }
}
