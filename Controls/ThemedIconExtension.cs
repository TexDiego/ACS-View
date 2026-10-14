using MauiIcons.Core;
using MauiIcons.Fluent;
using Microsoft.Maui.Controls.Xaml;

namespace ACS_View.Controls;

public enum IconRole { Surface, Primary, Danger, OnFill, OnContainer }

// Native FontImageSource retains its theme binding after XAML loading.
// Filled controls use OnFill, regardless of the current theme.
[AcceptEmptyServiceProvider]
public sealed class ThemedIconExtension : IMarkupExtension<ImageSource>
{
    public FluentIcons Icon { get; set; }
    public double IconSize { get; set; } = 30;
    public IconRole Role { get; set; } = IconRole.Surface;

    public ImageSource ProvideValue(IServiceProvider serviceProvider)
    {
        var (lightKey, darkKey) = Role switch
        {
            IconRole.Primary => ("IconPrimary", "DarkIconPrimary"),
            IconRole.Danger => ("IconDanger", "DarkIconDanger"),
            IconRole.OnFill => ("OnPrimary", "OnPrimary"),
            IconRole.OnContainer => ("OnPrimaryContainer", "DarkOnPrimaryContainer"),
            _ => ("IconForeground", "DarkIconForeground")
        };
        var resources = Microsoft.Maui.Controls.Application.Current!.Resources;
        var light = (Color)resources[lightKey];
        var dark = (Color)resources[darkKey];
        var source = (FontImageSource)Icon.ToImageSource(light, IconSize, false);
        source.SetAppThemeColor(FontImageSource.ColorProperty, light, dark);
        return source;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
