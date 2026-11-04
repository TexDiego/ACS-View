namespace ACS_View.Controls;

/// <summary>Opt-in desktop layouts; mobile keeps the layout declared in XAML.</summary>
public static class DesktopLayout
{
    public static readonly BindableProperty MetricGridProperty = BindableProperty.CreateAttached(
        "MetricGrid", typeof(bool), typeof(DesktopLayout), false, propertyChanged: OnMetricGridChanged);

    public static bool GetMetricGrid(BindableObject view) => (bool)view.GetValue(MetricGridProperty);
    public static void SetMetricGrid(BindableObject view, bool value) => view.SetValue(MetricGridProperty, value);

    private static void OnMetricGridChanged(BindableObject bindable, object oldValue, object newValue)
    {
#if WINDOWS
        if (bindable is not CollectionView collection) return;
        collection.SizeChanged -= UpdateMetricGrid;
        if (!(bool)newValue) return;
        collection.ItemsLayout = new GridItemsLayout(1, ItemsLayoutOrientation.Vertical)
        {
            HorizontalItemSpacing = 8,
            VerticalItemSpacing = 8
        };
        collection.SizeChanged += UpdateMetricGrid;
        UpdateMetricGrid(collection, EventArgs.Empty);
#endif
    }

#if WINDOWS
    private static void UpdateMetricGrid(object? sender, EventArgs e)
    {
        if (sender is CollectionView { ItemsLayout: GridItemsLayout layout, Width: > 0 } collection)
        {
            // Leave room for a scrollbar, card padding and two lines of title text.
            var columns = Math.Clamp((int)((collection.Width - 24) / 320), 1, 4);
            if (layout.Span != columns) layout.Span = columns;
        }
    }
#endif
}
