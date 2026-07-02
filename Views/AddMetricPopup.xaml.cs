using ACS_View.Application.DTOs;
using ACS_View.Domain.ValueObjects;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class AddMetricPopup : Popup<DashboardMetricCreateRequestDto>
{
    private readonly AddMetricPopupViewModel _viewModel;

    public AddMetricPopup(
        IEnumerable<Dashboard> candidates,
        Func<DashboardMetricCreateRequestDto, string?>? validateRequest = null)
    {
        InitializeComponent();
        BindingContext = _viewModel = new AddMetricPopupViewModel(candidates, validateRequest);
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async void AddButton_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.TryCreateRequest(out var request, out _))
        {
            return;
        }

        await CloseAsync(request);
    }
}
