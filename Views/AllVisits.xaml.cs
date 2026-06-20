using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class AllVisits : ContentPage
{
    private readonly AllVisitsViewModel _viewModel;

    public AllVisits(AllVisitsViewModel _viewModel)
	{
		InitializeComponent();
        BindingContext = this._viewModel = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadVisitsAsync();
    }
}
