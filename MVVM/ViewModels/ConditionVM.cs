using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class ConditionVM : BaseViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [ObservableProperty] private bool isSelected = false;
    }
}