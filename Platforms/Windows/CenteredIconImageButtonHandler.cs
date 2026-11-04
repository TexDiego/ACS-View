using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using Font = Microsoft.Maui.Font;

namespace ACS_View.Platforms.Windows;

// Render font glyphs directly. A rasterized FontImageSource can include font
// baseline whitespace, which cannot be corrected with the button's padding.
public sealed class CenteredIconImageButtonHandler : ImageButtonHandler, IImageHandler
{
    private Microsoft.UI.Xaml.Controls.Image? rasterContent;
    Microsoft.UI.Xaml.Controls.Image IImageHandler.PlatformView => rasterContent!;
    private FontImageSource? observedSource;
    public static readonly IPropertyMapper<IImageButton, IImageButtonHandler> IconMapper =
        new PropertyMapper<IImageButton, IImageButtonHandler>(ImageButtonHandler.Mapper)
        {
            [nameof(Microsoft.Maui.IImage.Source)] = MapIcon
        };

    public CenteredIconImageButtonHandler() : base(IconMapper) { }

    protected override Microsoft.UI.Xaml.Controls.Button CreatePlatformView()
    {
        var button = base.CreatePlatformView();
        rasterContent = (Microsoft.UI.Xaml.Controls.Image)button.Content;
        return button;
    }

    private static void MapIcon(IImageButtonHandler handler, IImageButton button)
    {
        if (handler is not CenteredIconImageButtonHandler self) return;
        if (self.observedSource is not null) self.observedSource.PropertyChanged -= self.SourceChanged;
        self.observedSource = (button as ImageButton)?.Source as FontImageSource;
        if (self.observedSource is null)
        {
            self.PlatformView.Content = self.rasterContent;
            ImageHandler.MapSource(handler, button);
            return;
        }
        self.observedSource.PropertyChanged += self.SourceChanged;
        self.UpdateIcon();
    }

    private void SourceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => UpdateIcon();

    private void UpdateIcon()
    {
        if (observedSource is not { } source || MauiContext is null) return;
        var manager = MauiContext.Services.GetRequiredService<IFontManager>();
        PlatformView.HorizontalContentAlignment = HorizontalAlignment.Center;
        PlatformView.VerticalContentAlignment = VerticalAlignment.Center;
        PlatformView.Content = new FontIcon
        {
            Glyph = source.Glyph,
            FontFamily = manager.GetFontFamily(Font.OfSize(source.FontFamily, source.Size)),
            FontSize = source.Size,
            Foreground = source.Color.ToPlatform(),
            IsTextScaleFactorEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    protected override void DisconnectHandler(Microsoft.UI.Xaml.Controls.Button platformView)
    {
        if (observedSource is not null) observedSource.PropertyChanged -= SourceChanged;
        observedSource = null;
        rasterContent = null;
        base.DisconnectHandler(platformView);
    }
}
