using ACS_View.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.ViewModels
{
    internal partial class DashboardItemVM : BaseViewModel
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public DashboardItemType ItemType { get; set; }

        [ObservableProperty] private string name;

        [ObservableProperty] private int total;

        [ObservableProperty] private int displayOrder;
    }
}