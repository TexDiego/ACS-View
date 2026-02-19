using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class OverallView : ContentPage
{
    private readonly OverallViewModel _overallViewModel = new();

    public OverallView()
    {
        InitializeComponent();
        BindingContext = _overallViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_overallViewModel.LoadSummaryCommand.CanExecute(null))
            _overallViewModel.LoadSummaryCommand.Execute(null);
    }
}