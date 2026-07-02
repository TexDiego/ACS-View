using ACS_View.Application.DTOs;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class OverallMetricMenuPopup : Popup<OverallMetricMenuAction>
{
    public OverallMetricMenuPopup(bool hasHiddenMetrics, bool canAddMetric)
    {
        InitializeComponent();
        BindingContext = new OverallMetricMenuPopupViewModel(hasHiddenMetrics, canAddMetric);
    }

    private async void OpenCidsButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(OverallMetricMenuAction.OpenCids);
    }

    private async void RestoreHiddenMetricsButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(OverallMetricMenuAction.RestoreHiddenMetrics);
    }

    private async void AddMetricButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(OverallMetricMenuAction.AddMetric);
    }
}
