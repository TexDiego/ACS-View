using ACS_View.Domain.Entities;
using ACS_View.Application.DTOs;
using ACS_View.Domain.Enums;

namespace ACS_View.Application.Interfaces
{
    public interface IVisitsService
    {
        Task<PatientVisitSummaryDto> GetPatientVisitSummaryAsync(int patientId, DateTime referenceDate);
        Task<List<Visits>> GetAllVisitsAsync();
        Task<VisitMonthlyOverviewDto> GetMonthlyOverviewAsync(DateTime month);
        Task<List<VisitFamilyMemberOptionDto>> GetFamilyVisitOptionsAsync(int houseId, int familyId, DateTime visitDate);
        Task<List<VisitRecordFamilyGroupDto>> GetVisitRecordGroupsAsync(DateTime month, VisitStatus? status);
        Task<List<VisitSuggestionDto>> GetVisitSuggestionsAsync(DateTime referenceDate);
        Task PurgeExpiredVisitsAsync(DateTime referenceDate);
        Task RegisterVisitAsync(Visits visit);
        Task<bool> RegisterQuickVisitAsync(int patientId, Func<string, string, string, Task<bool>> confirm);
        Task<int> RegisterFamilyVisitBatchAsync(VisitBatchRequestDto request);
        Task DeleteVisitAsync(int id);
    }
}
