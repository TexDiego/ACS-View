using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.ViewModels;

public sealed class SexFilterOption(string value) : ObservableObject
{
    private bool isSelected;

    public string Value { get; } = value;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
