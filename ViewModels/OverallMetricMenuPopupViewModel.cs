namespace ACS_View.ViewModels;

public sealed class OverallMetricMenuPopupViewModel(
    bool hasHiddenMetrics,
    bool canAddMetric) : BaseViewModel
{
    public bool HasHiddenMetrics { get; } = hasHiddenMetrics;
    public bool CanAddMetric { get; } = canAddMetric;
}
