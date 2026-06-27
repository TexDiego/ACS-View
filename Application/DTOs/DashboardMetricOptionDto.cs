using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.Application.DTOs;

public partial class DashboardMetricOptionDto : ObservableObject
{
    public required Dashboard Metric { get; init; }
    public string Name => Metric.Name;
    public string FilterKey => Metric.FilterKey;

    [ObservableProperty] private bool isSelected;
}
