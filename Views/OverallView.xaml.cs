using ACS_View.Domain.Interfaces;
using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class OverallView : ContentPage
{
    private readonly OverallViewModel _viewModel;

    public OverallView(IDashboardMetricsService dash)
    {
        InitializeComponent();
        BindingContext = _viewModel =  new OverallViewModel(dash);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.LoadSummaryCommand.CanExecute(null))
            _viewModel.LoadSummaryCommand.Execute(null);
    }
}