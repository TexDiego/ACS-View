using ACS_View.Enums;
using ACS_View.MVVM.Models.DTOs;

namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface IDashboardService
    {
        Task AddItemAsync(DashboardItemType type, int itemId);
        Task RemoveItemAsync(int id);
        Task UpdateOrderAsync(List<DashboardItem> items);
        Task<List<DashboardItemResult>> GetDashboardSummaryAsync();
    }
}