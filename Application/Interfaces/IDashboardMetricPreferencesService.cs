using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces;

public interface IDashboardMetricPreferencesService
{
    Task<DashboardMetricPreferencesDto> GetAsync();
    Task SaveAsync(DashboardMetricPreferencesDto preferences);
}
