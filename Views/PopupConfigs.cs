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
                CornerRadius = new CornerRadius(15),
                StrokeThickness = 2,
                Stroke = ThemeColors.BorderSoft,
                BackgroundColor = ThemeColors.Surface
            },
            Shadow = null
        };
    }
}
