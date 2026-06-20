using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.Domain.ValueObjects
{
    public partial class CommonConditions : ObservableObject
    {
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string cid = string.Empty;
        [ObservableProperty] private bool isSelected = false;
    }
}