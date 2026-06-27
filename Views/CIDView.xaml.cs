using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class CIDView : ContentPage
{
    private readonly CIDViewViewModel _viewModel;

    public CIDView(CIDViewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.InitializeAsync();
    }

    private void CollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
    }

    private void BackToTopButton_Clicked(object sender, EventArgs e)
    {
        ScrollToTopButtonController.ScrollToTop(CidCollectionView, BackToTopButton);
    }
}
