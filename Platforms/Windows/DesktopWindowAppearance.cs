using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ACS_View.Platforms.Windows;

internal static class DesktopWindowAppearance
{
    public static void UpdateNavigation(Microsoft.UI.Xaml.DependencyObject root)
    {
        foreach (var item in Descendants(root).OfType<NavigationViewItem>())
        {
            item.Loaded -= NavigationItemLoaded;
            item.Loaded += NavigationItemLoaded;
            item.SizeChanged -= NavigationItemSizeChanged;
            item.SizeChanged += NavigationItemSizeChanged;
            item.FontSize = 16;
            item.MinHeight = 48;
            item.Padding = new Microsoft.UI.Xaml.Thickness(12, 8, 12, 8);
            if (item.Icon is { } icon)
            {
                icon.Width = 24;
                icon.Height = 24;
                if (icon is FontIcon font) font.FontSize = 24;
                if (icon is IconSourceElement { IconSource: FontIconSource source }) source.FontSize = 24;
            }
            UpdateNavigationItemVisuals(item);
        }
    }

    private static void NavigationItemLoaded(object sender, RoutedEventArgs e) => UpdateNavigationItemVisuals((NavigationViewItem)sender);
    private static void NavigationItemSizeChanged(object sender, SizeChangedEventArgs e) => UpdateNavigationItemVisuals((NavigationViewItem)sender);

    private static void UpdateNavigationItemVisuals(NavigationViewItem item)
    {
        if (item.Icon is { } icon)
        {
            // WinUI's top navigation template constrains its icon presenter even
            // when a larger source is supplied. Scale within the padded item.
            icon.RenderTransformOrigin = new global::Windows.Foundation.Point(.5, .5);
            icon.RenderTransform = new ScaleTransform { ScaleX = 1.4, ScaleY = 1.4 };
        }
        // Templates are materialized after Shell.Loaded, and icons may load later.
        foreach (var visual in Descendants(item))
        {
            if (visual is Microsoft.UI.Xaml.Controls.Image image)
            {
                image.Width = 24;
                image.Height = 24;
            }
            if (visual is FontIcon glyph) glyph.FontSize = 24;
            if (visual is TextBlock { Text.Length: 1 } text && char.GetUnicodeCategory(text.Text[0]) == System.Globalization.UnicodeCategory.PrivateUse)
                text.FontSize = 24;
        }
    }

    private static IEnumerable<Microsoft.UI.Xaml.DependencyObject> Descendants(Microsoft.UI.Xaml.DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    public static void ConfigureToolbar()
    {
        LabelHandler.Mapper.AppendToMapping(nameof(ILabel.Font), (handler, _) =>
            handler.PlatformView.FontSize = Math.Max(16, handler.PlatformView.FontSize));
        ButtonHandler.Mapper.AppendToMapping(nameof(ITextStyle.Font), (handler, button) =>
        {
            handler.PlatformView.FontSize = Math.Max(16, handler.PlatformView.FontSize);
            if (button is Microsoft.Maui.Controls.Button { StyleId: "FamilyMember" })
                handler.PlatformView.HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
        });
        ToolbarHandler.Mapper.AppendToMapping(nameof(IToolbar.Title), (handler, _) => ApplyToolbar(handler.PlatformView));
        ToolbarHandler.Mapper.AppendToMapping("DesktopSpacing", (handler, _) =>
        {
            var bar = handler.PlatformView;
            bar.Padding = new Microsoft.UI.Xaml.Thickness(12, 4, 12, 4);
            bar.Loaded += (_, _) => ApplyToolbar(bar);
            bar.PrimaryCommands.VectorChanged += (_, _) => ApplyToolbar(bar);
        });
    }

    private static void ApplyToolbar(CommandBar bar)
    {
        if (bar.FindName("title") is TextBlock title)
        {
            title.FontSize = 22;
            title.Text = title.Text.Replace('\r', ' ').Replace('\n', ' ');
            title.Margin = new Microsoft.UI.Xaml.Thickness(20, 0, 12, 0);
        }
        foreach (var button in bar.PrimaryCommands.OfType<AppBarButton>())
        {
            button.MinWidth = 48;
            button.MinHeight = 48;
            button.FontSize = 16;
            button.Padding = new Microsoft.UI.Xaml.Thickness(10, 6, 10, 6);
            if (button.Icon is BitmapIcon bitmap)
            {
                bitmap.Width = 26;
                bitmap.Height = 26;
            }
            if (button.Icon is FontIcon font) font.FontSize = 26;
            foreach (var image in Descendants(button).OfType<Microsoft.UI.Xaml.Controls.Image>())
            {
                image.Width = 26;
                image.Height = 26;
            }
        }
    }

    public static void Attach(Microsoft.UI.Xaml.Window window)
    {
        var app = Microsoft.Maui.Controls.Application.Current!;
        void Apply()
        {
            var dark = app.RequestedTheme == AppTheme.Dark;
            var background = ((Color)app.Resources[dark ? "DarkPrimaryLight" : "Primary"]).ToWindowsColor();
            var titleBar = window.AppWindow.TitleBar;
            titleBar.BackgroundColor = background;
            titleBar.InactiveBackgroundColor = background;
            titleBar.ForegroundColor = Microsoft.UI.Colors.White;
            titleBar.InactiveForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonBackgroundColor = background;
            titleBar.ButtonInactiveBackgroundColor = background;
            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonHoverBackgroundColor = ((Color)app.Resources["PrimaryDark"]).ToWindowsColor();
        }
        void ThemeChanged(object? sender, AppThemeChangedEventArgs e) => window.DispatcherQueue.TryEnqueue(Apply);
        app.RequestedThemeChanged += ThemeChanged;
        window.Closed += (_, _) => app.RequestedThemeChanged -= ThemeChanged;
        Apply();
    }
}
