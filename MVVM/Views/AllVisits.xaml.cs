using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class AllVisits : ContentPage
{
    private readonly AllVisitsViewModel _viewModel = new();

    public AllVisits()
	{
		InitializeComponent();
        BindingContext = _viewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadVisitsAsync();
    }
}