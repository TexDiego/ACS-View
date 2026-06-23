using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces
{
    public interface IDashboardMetricsService
    {
        Task<DashboardMetricsDto> GetMetricsAsync();
        Task<List<DashboardCidsDTO>> GetCidMetricsAsync();
        Task<List<ConditionsDTO>> GetConditionsAsync();
        Task<int> CountPatientsByFilterAsync(string filterKey);
    }
}
