using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class HousesPage : ContentPage
{
    private readonly HousesPageViewModel _viewModel;

    public HousesPage(HousesPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.LoadHousesCommand.Execute(e.NewTextValue);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadInitialHousesAsync();
    }
}
