using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class AllVisits : ContentPage
{
    private readonly AllVisitsViewModel _viewModel;

    public AllVisits()
	{
		InitializeComponent();

        _viewModel = new AllVisitsViewModel();

        BindingContext = _viewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadVisitsAsync();
    }
}