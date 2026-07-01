using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Shapes;

namespace ACS_View.Views
{
    internal static class PopupConfigs
    {
        internal static IPopupOptions Default => new PopupOptions
        {
            CanBeDismissedByTappingOutsideOfPopup = true,
            PageOverlayColor = ThemeColors.PopupOverlay,
            Shape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(0),
                StrokeThickness = 0,
                Stroke = Colors.Transparent,
                BackgroundColor = Colors.Transparent
            },
            Shadow = null
        };
    }
}
