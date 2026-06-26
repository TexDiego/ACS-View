using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.Domain.ValueObjects
{
    public partial class Dashboard : ObservableObject
    {
        [ObservableProperty] private string name;
        [ObservableProperty] private string cid;
        [ObservableProperty] private string filterKey;
        [ObservableProperty] private string summary = "Resumo geral";
        [ObservableProperty] private int total;
        [ObservableProperty] private bool isCombination;
    }
}
