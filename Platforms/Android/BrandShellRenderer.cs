using Android.Content.Res;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;

namespace ACS_View.Platforms.Android;

public sealed class BrandShellRenderer : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
        => new BrandTabBarAppearanceTracker(this, shellItem);

    private sealed class BrandTabBarAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
        : ShellBottomNavViewAppearanceTracker(shellContext, shellItem)
    {
        public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
        {
            base.SetAppearance(bottomView, appearance);

            // Material 3's selection pill has its own color, independent of
            // Shell.TabBarForegroundColor and Shell.TabBarBackgroundColor.
            bottomView.ItemActiveIndicatorColor = ColorStateList.ValueOf(
                ThemeColors.TabBarSelectedIndicator.ToPlatform());
        }
    }
}
