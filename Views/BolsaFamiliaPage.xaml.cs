using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class BolsaFamiliaPage : ContentPage
{
    private readonly BolsaFamiliaPageViewModel _viewModel;

    public BolsaFamiliaPage(BolsaFamiliaPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.ShouldSkipTransientReload())
        {
            return;
        }

        _ = _viewModel.LoadGroupsAsync();
    }
}
