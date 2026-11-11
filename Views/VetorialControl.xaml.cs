using ACS_View.ViewModels;
#if WINDOWS
using WindowsXaml = Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#endif

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
#if WINDOWS
        if (VectorControlCollectionView.Handler?.PlatformView is WindowsXaml.FrameworkElement list &&
            FindScrollViewer(list) is { } scrollViewer)
        {
            // An active scroll can settle at its current offset before WinUI accepts the jump.
            void FinishAtTop(object? sender, WindowsXaml.Controls.ScrollViewerViewChangedEventArgs args)
            {
                if (args.IsIntermediate)
                {
                    return;
                }

                scrollViewer.ViewChanged -= FinishAtTop;
                if (scrollViewer.VerticalOffset > 0)
                {
                    scrollViewer.DispatcherQueue.TryEnqueue(() => scrollViewer.ChangeView(null, 0, null, disableAnimation: true));
                }
            }

            scrollViewer.ViewChanged += FinishAtTop;
            if (scrollViewer.Content is WindowsXaml.UIElement content)
            {
                content.CancelDirectManipulations();
            }

            scrollViewer.ChangeView(null, 0, null, disableAnimation: true);
            return;
        }
#elif ANDROID
        if (VectorControlCollectionView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recycler)
        {
            recycler.StopScroll();
        }
#endif
        VectorControlCollectionView.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
        BackToTopButton.IsVisible = false;
    }

#if WINDOWS
    private static WindowsXaml.Controls.ScrollViewer? FindScrollViewer(WindowsXaml.DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var scrollViewer = child as WindowsXaml.Controls.ScrollViewer ?? FindScrollViewer(child);
            if (scrollViewer is not null)
            {
                return scrollViewer;
            }
        }

        return null;
    }
#endif
}
