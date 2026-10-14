using ACS_View.Application.Interfaces;
using Microsoft.Maui.Controls.Shapes;

namespace ACS_View.Controls;

/// <summary>Reusable, non-modal message surface displayed by ISnackbarService.</summary>
public sealed class AppSnackbar : ContentView
{
    public AppSnackbar(string message, SnackbarStyle style)
    {
        var isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        var label = new Label
        {
            Text = message,
            TextColor = style == SnackbarStyle.Error
                ? (isDark ? ThemeColors.PageBackground : Colors.White) : ThemeColors.Surface,
            FontSize = 14,
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalOptions = LayoutOptions.Center
        };
        SemanticProperties.SetDescription(this, message);
        Content = new Border
        {
            Content = label,
            BackgroundColor = style == SnackbarStyle.Error ? ThemeColors.Danger : ThemeColors.TextPrimary,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
            Padding = new Thickness(16, 12)
        };
        Margin = new Thickness(16, 0, 16, 24);
        MaximumWidthRequest = 560;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.End;
        ZIndex = 1000;
    }
}
