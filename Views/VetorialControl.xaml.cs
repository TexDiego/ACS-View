using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class VetorialControl : ContentPage
{
	public VetorialControl(VetorialControlViewModel viewModel)
	{
		BindingContext = viewModel;
        InitializeComponent();
	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        InputFocusGuard.ClearTextInputFocus(this);
        await ((VetorialControlViewModel)BindingContext).LoadAsync();
    }
    protected override void OnDisappearing()
    {
        ((VetorialControlViewModel)BindingContext).CancelPendingSearch();
        InputFocusGuard.ClearTextInputFocus(this);
        base.OnDisappearing();
    }

    private void CollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
#if ANDROID
        // MAUI accumulates scroll deltas on Android. A jump can leave that offset stale.
        if (VectorControlCollectionView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recycler)
        {
            BackToTopButton.IsVisible = recycler.CanScrollVertically(-1);
            return;
        }
#endif
        BackToTopButton.IsVisible = e.VerticalOffset > 0;
    }

    private void BackToTopButton_Clicked(object sender, EventArgs e)
    {
        VectorControlCollectionView.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
        BackToTopButton.IsVisible = false;
    }
}
