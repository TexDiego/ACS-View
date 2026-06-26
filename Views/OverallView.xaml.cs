using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class OverallView : ContentPage
{
    private readonly OverallViewModel _viewModel;

    public OverallView(OverallViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.LoadSummaryCommand.CanExecute(null))
            _viewModel.LoadSummaryCommand.Execute(null);
    }
}
