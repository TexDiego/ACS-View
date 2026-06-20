using ACS_View.UseCases.DTOs;

namespace ACS_View.Domain.Interfaces
{
    public interface IDashboardMetricsService
    {
        Task<DashboardMetricsDto> GetMetricsAsync();
        Task<List<DashboardCidsDTO>> GetCidMetricsAsync();
        Task<List<ConditionsDTO>> GetConditionsAsync();
        Task<int> CountPatientsByFilterAsync(string filterKey);
    }
}
