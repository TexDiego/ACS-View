using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.Domain.ValueObjects
{
    internal partial class Dashboard : ObservableObject
    {
        [ObservableProperty] private string name;
        [ObservableProperty] private int total;
    }
}