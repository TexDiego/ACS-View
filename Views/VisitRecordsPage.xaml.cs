using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class VisitRecordsPage : ContentPage
{
    private readonly VisitRecordsViewModel viewModel;

    public VisitRecordsPage(VisitRecordsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = viewModel.LoadAsync(force: true);
    }

    private void CollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
    }

    private void BackToTopButton_Clicked(object sender, EventArgs e)
    {
        ScrollToTopButtonController.ScrollToTop(RecordsCollectionView, BackToTopButton);
    }
}
