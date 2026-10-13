using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IRelationalDataService
{
    Task<List<PatientPhone>> GetPhonesAsync(int patientId);
    Task SavePhoneAsync(PatientPhone phone);
    Task DeletePhoneAsync(int id);
    Task<List<PatientSusNumber>> GetSusNumbersAsync(int patientId);
    Task<List<FamilyMember>> GetMembershipHistoryAsync(int patientId);
    Task<List<FamilyResidenceHistory>> GetResidenceHistoryAsync(int familyId);
    Task AssignTerritoryAsync(TerritorialAssignment assignment);
    Task<List<MigrationIssue>> GetReconciliationIssuesAsync();
}
