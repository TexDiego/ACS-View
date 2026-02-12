using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class HousesPage : ContentPage
{
    private readonly HousesPageViewModel _viewModel = new();

    public HousesPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.LoadHousesCommand.Execute(e.NewTextValue);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadHousesCommand.Execute(null);
    }
}
