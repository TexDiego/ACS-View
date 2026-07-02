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

    private void CollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
    }

    private void BackToTopButton_Clicked(object sender, EventArgs e)
    {
        ScrollToTopButtonController.ScrollToTop(BolsaFamiliaCollectionView, BackToTopButton);
    }
}
