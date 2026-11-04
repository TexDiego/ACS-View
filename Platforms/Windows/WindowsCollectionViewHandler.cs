using Microsoft.Maui.Controls.Handlers.Items;

namespace ACS_View.Platforms.Windows;

/// <summary>
/// Gives WinUI a layout root which measures the complete item, including its margins.
/// Keeping the card inside that layout also constrains wrapping text to the viewport.
/// </summary>
public sealed class WindowsCollectionViewHandler : CollectionViewHandler
{
    protected override void UpdateItemTemplate()
    {
        if (VirtualView.ItemTemplate is { } template && template is not WindowsItemTemplateSelector)
        {
            // Setting the template re-enters this mapping. The selector guard lets that
            // call perform the native update once, including when a template is replaced.
            VirtualView.ItemTemplate = new WindowsItemTemplateSelector(template, VirtualView);
            return;
        }

        base.UpdateItemTemplate();
    }

    private sealed class WindowsItemTemplateSelector(DataTemplate source, ReorderableItemsView collection) : DataTemplateSelector
    {
        private readonly Dictionary<DataTemplate, DataTemplate> templates = new();

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var selected = source is DataTemplateSelector selector
                ? selector.SelectTemplate(item, container)
                : source;

            if (!templates.TryGetValue(selected, out var wrapped))
            {
                wrapped = new DataTemplate(() =>
                {
                    var content = (View)selected.CreateContent();
                    var horizontal = collection.ItemsLayout is LinearItemsLayout
                    {
                        Orientation: ItemsLayoutOrientation.Horizontal
                    };

                    // Padding belongs to the measured item, so WinUI reserves actual
                    // space even for templates whose root has no external margin.
                    var layout = new Grid
                    {
                        Padding = horizontal ? new Thickness(4, 2) : new Thickness(4),
                        RowDefinitions = { new RowDefinition(GridLength.Auto) },
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star) }
                    };
                    layout.Children.Add(content);
                    return layout;
                });
                templates.Add(selected, wrapped);
            }

            return wrapped;
        }
    }
}
