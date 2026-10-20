using System.Text.RegularExpressions;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.Migrations;

namespace ACS_View.UseCases.Services;

internal sealed class RelationalDataService(IDatabaseService database, ICurrentUserContext context) : IRelationalDataService
{
    public Task<List<PatientPhone>> GetPhonesAsync(int patientId)
    {
        var user=context.RequireCurrentUserId();
        return database.Connection.Table<PatientPhone>().Where(x=>x.UserId==user && x.PatientId==patientId).ToListAsync();
    }
    public Task<List<PatientSusNumber>> GetSusNumbersAsync(int patientId)
    {
        var user=context.RequireCurrentUserId();
        return database.Connection.Table<PatientSusNumber>().Where(x=>x.UserId==user && x.PatientId==patientId).OrderBy(x=>x.OriginalOrder).ToListAsync();
    }
    public Task<List<FamilyMember>> GetMembershipHistoryAsync(int patientId)
    {
        var user=context.RequireCurrentUserId();
        return database.Connection.Table<FamilyMember>().Where(x=>x.UserId==user && x.PatientId==patientId).OrderBy(x=>x.Id).ToListAsync();
    }
    public Task<List<FamilyResidenceHistory>> GetResidenceHistoryAsync(int familyId)
    {
        var user=context.RequireCurrentUserId();
        return database.Connection.Table<FamilyResidenceHistory>().Where(x=>x.UserId==user && x.FamilyId==familyId).OrderBy(x=>x.Id).ToListAsync();
    }
    public Task<List<MigrationIssue>> GetReconciliationIssuesAsync()
    {
        var user=context.RequireCurrentUserId();
        return database.Connection.Table<MigrationIssue>().Where(x=>x.UserId==user).ToListAsync();
    }
    public Task SavePhoneAsync(PatientPhone phone)
    {
        var user=context.RequireCurrentUserId();
        if(string.IsNullOrWhiteSpace(phone.Number))throw new ArgumentException("Informe o telefone.");
        var digits=Regex.Replace(phone.Number,@"\D","");
        phone.NormalizedNumber=digits.Length is >=8 and <=15 ? digits : null;
        phone.UserId=user;
        return database.Connection.RunInTransactionAsync(c=>
        {
            if(c.Table<Patient>().FirstOrDefault(p=>p.Id==phone.PatientId && p.UserId==user)==null)
                throw new InvalidOperationException("Paciente não pertence à conta atual.");
            if(phone.Id!=0 && c.Table<PatientPhone>().FirstOrDefault(p=>p.Id==phone.Id && p.UserId==user && p.PatientId==phone.PatientId)==null)
                throw new InvalidOperationException("Telefone não pertence ao paciente atual.");
            if(phone.IsPrimary)c.Execute("UPDATE PatientPhone SET IsPrimary=0 WHERE PatientId=? AND UserId=?",phone.PatientId,user);
            if(phone.Id==0)c.Insert(phone);else c.Update(phone);
            if(phone.NormalizedNumber==null)RelationalStore.Issue(c,user,"PatientPhone",phone.Id,"IncompletePhone",phone.Number);
        });
    }
    public Task DeletePhoneAsync(int id) => database.Connection.ExecuteAsync("DELETE FROM PatientPhone WHERE Id=? AND UserId=?",id,context.RequireCurrentUserId());
    public async Task AssignTerritoryAsync(TerritorialAssignment assignment)
    {
        var user=context.RequireCurrentUserId();
        if(assignment.Id!=0)throw new ArgumentException("Crie uma nova atribuição para preservar o histórico.");
        assignment.CensusSector=TerritorialRules.OptionalCode(assignment.CensusSector) ?? "";
        assignment.BlockNumber=TerritorialRules.OptionalCode(assignment.BlockNumber) ?? "";
        assignment.UserId=user;assignment.ValidFrom=DateTime.UtcNow;assignment.ValidTo=null;
        await database.Connection.RunInTransactionAsync(c=>
        {
            if(c.Table<House>().FirstOrDefault(h=>h.CasaId==assignment.HouseId && h.UserId==user && h.IsActive)==null)
                throw new InvalidOperationException("Residência não pertence à conta atual.");
            c.Execute("UPDATE TerritorialAssignment SET ValidTo=? WHERE HouseId=? AND UserId=? AND ValidTo IS NULL",assignment.ValidFrom,assignment.HouseId,user);
            c.Insert(assignment);
            c.Execute("UPDATE House SET SetorCensitario=?, NumeroQuadra=? WHERE CasaId=? AND UserId=?",
                TerritorialRules.OptionalCode(assignment.CensusSector), TerritorialRules.OptionalCode(assignment.BlockNumber), assignment.HouseId, user);
        });
        DataChangeTracker.MarkHousesChanged();
    }
}
