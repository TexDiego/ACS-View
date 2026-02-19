using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class ConditionCategoryVM : BaseViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [ObservableProperty] private ObservableCollection<ConditionVM> conditions = [];
    }
}