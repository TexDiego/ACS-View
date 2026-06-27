using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.Application.DTOs;

public sealed partial class CidCodePrefixFilterDto : ObservableObject
{
    public string Prefix { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    [ObservableProperty] private bool isSelected;
}
