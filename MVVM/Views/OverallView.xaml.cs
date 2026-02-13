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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_overallViewModel.LoadSummaryCommand.CanExecute(null))
            await _overallViewModel.LoadSummaryCommand.ExecuteAsync(null);
    }
}