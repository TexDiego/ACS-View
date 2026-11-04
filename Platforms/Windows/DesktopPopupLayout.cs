using CommunityToolkit.Maui.Views;

namespace ACS_View.Platforms.Windows;

/// <summary>Bounds popups to their owning window and releases resize subscriptions on close.</summary>
internal sealed class DesktopPopupLayout : IDisposable
{
    private readonly Popup popup;
    private readonly Window window;
    private readonly View content;
    private readonly ScrollView? viewport;
    private readonly bool hasFixedViewport;
    private readonly Dictionary<View, double> requestedHeights = new();
    private readonly Dictionary<View, double> maximumHeights = new();

    public DesktopPopupLayout(Popup popup, Window window)
    {
        this.popup = popup;
        this.window = window;
        content = popup.Content;
        hasFixedViewport = popup is Views.FilterPopup or Views.ParentPatientPickerPopup
            or Views.VisitPage or Views.AddMetricPopup;

        if (!hasFixedViewport)
        {
            // Long messages and forms remain reachable on a short window. Lists
            // inside this viewport receive their own finite height below.
            popup.Content = null;
            viewport = new ScrollView { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Default };
            popup.Content = viewport;
        }

        popup.HorizontalOptions = LayoutOptions.Center;
        popup.VerticalOptions = LayoutOptions.Center;
        window.SizeChanged += OnSizeChanged;
        popup.Loaded += OnSizeChanged;
        Resize();
    }

    private void OnSizeChanged(object? sender, EventArgs e) => Resize();

    private void Resize()
    {
        var availableWidth = Math.Max(1, window.Width - 80);
        var availableHeight = Math.Max(1, window.Height - 144);
        var preferredWidth = popup switch
        {
            Views.DialogPopup => 480,
            Views.OverallMetricMenuPopup => 480,
            Views.HouseFilterPopup => 720,
            Views.FilterPopup => 860,
            Views.PersonsInfo => 960,
            _ => 960
        };
        popup.WidthRequest = Math.Min(preferredWidth, availableWidth * .9);
        popup.MaximumHeightRequest = availableHeight;
        popup.HeightRequest = hasFixedViewport ? Math.Min(820, availableHeight) : -1;

        if (viewport is not null) viewport.MaximumHeightRequest = availableHeight;

        foreach (var view in Descendants(content))
        {
            requestedHeights.TryAdd(view, view.HeightRequest);
            maximumHeights.TryAdd(view, view.MaximumHeightRequest);

            if (requestedHeights[view] > 0)
                view.HeightRequest = hasFixedViewport && view is Layout
                    ? Math.Min(820, availableHeight) - 32
                    : Math.Min(requestedHeights[view], availableHeight - 32);

            if (view is CollectionView list && !hasFixedViewport)
            {
                var height = Math.Min(480, availableHeight * .55);
                if (popup is Views.PersonsInfo && list.ItemsSource is System.Collections.ICollection items)
                    height = Math.Min(height, Math.Ceiling(items.Count / 5d) * 62 + 16);
                list.HeightRequest = height;
            }
            else if (view is ScrollView || (view is Layout or Border && maximumHeights[view] < double.PositiveInfinity))
            {
                view.MaximumHeightRequest = availableHeight - 32;
            }
        }

        if (hasFixedViewport)
        {
            content.HeightRequest = popup.HeightRequest;
            content.MaximumHeightRequest = popup.HeightRequest;
        }
    }

    private static IEnumerable<View> Descendants(View root)
    {
        yield return root;
        foreach (var child in ((IVisualTreeElement)root).GetVisualChildren().OfType<View>())
            foreach (var descendant in Descendants(child)) yield return descendant;
    }

    public void Dispose()
    {
        window.SizeChanged -= OnSizeChanged;
        popup.Loaded -= OnSizeChanged;
    }
}
