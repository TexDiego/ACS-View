using ACS_View.UseCases.DTOs;

namespace ACS_View.Domain.Interfaces
{
    public interface IDashboardMetricsService
    {
        Task<DashboardMetricsDto> GetMetricsAsync();
    }
}