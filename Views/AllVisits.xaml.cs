using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class AllVisits : ContentPage
{
    private readonly AllVisitsViewModel _viewModel = new();

    public AllVisits()
	{
		InitializeComponent();
        BindingContext = _viewModel;
    }
}