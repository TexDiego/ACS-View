using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class AllVisits : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly AllVisitsViewModel _viewModel;

    public AllVisits()
	{
		InitializeComponent();

        _databaseService = new DatabaseService();
        _viewModel = new AllVisitsViewModel(_databaseService);

        BindingContext = _viewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadVisitsAsync();
    }
}