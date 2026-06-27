namespace ACS_View.Views;

internal static class ScrollToTopButtonController
{
    private const uint ShowAnimationLength = 120;
    private const uint HideAnimationLength = 90;
    private const double VisibleOpacity = 0.92;

    public static void UpdateVisibility(ImageButton button, ItemsViewScrolledEventArgs e)
    {
        SetVisible(button, e.FirstVisibleItemIndex > 0);
    }

    public static void ScrollToTop(CollectionView collectionView, ImageButton button)
    {
        collectionView.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
        SetVisible(button, false);
    }

    private static void SetVisible(ImageButton button, bool visible)
    {
        if (visible)
        {
            if (button.IsVisible)
            {
                return;
            }

            button.AbortAnimation(nameof(ScrollToTopButtonController));
            button.Opacity = 0;
            button.IsVisible = true;
            _ = button.FadeToAsync(VisibleOpacity, ShowAnimationLength, Easing.CubicOut);
            return;
        }

        if (!button.IsVisible)
        {
            return;
        }

        button.AbortAnimation(nameof(ScrollToTopButtonController));
        _ = HideAsync(button);
    }

    private static async Task HideAsync(ImageButton button)
    {
        await button.FadeToAsync(0, HideAnimationLength, Easing.CubicIn);
        button.IsVisible = false;
    }
}
