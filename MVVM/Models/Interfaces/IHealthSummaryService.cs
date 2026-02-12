using ACS_View.MVVM.Models.DTOs;

namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IHealthSummaryService
    {
        Task<HealthSummary> GetHealthSummaryAsync();
    }
}