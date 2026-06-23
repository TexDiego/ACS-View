namespace ACS_View
{
    internal static class ThemeColors
    {
        private static bool IsDark => Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;

        internal static Color PageBackground => Color.FromArgb(IsDark ? "#101820" : "#FFFFFF");
        internal static Color Surface => Color.FromArgb(IsDark ? "#17232D" : "#FFFFFF");
        internal static Color SurfaceMuted => Color.FromArgb(IsDark ? "#1F2C36" : "#F7FAFB");
        internal static Color TextPrimary => Color.FromArgb(IsDark ? "#EEF5F8" : "#1F2933");
        internal static Color BorderSoft => Color.FromArgb(IsDark ? "#2E4350" : "#D7E1E7");
        internal static Color BorderStrong => Color.FromArgb(IsDark ? "#42606F" : "#B7C7D0");
        internal static Color SelectedSurface => Color.FromArgb(IsDark ? "#153E4B" : "#DDF3FA");
        internal static Color SkeletonFill => Color.FromArgb(IsDark ? "#293946" : "#E5EBEF");
        internal static Color SkeletonShine => Color.FromArgb(IsDark ? "#33FFFFFF" : "#FFFFFFFF");
        internal static Color PopupOverlay => Color.FromArgb(IsDark ? "#99071116" : "#4D6B7680");
        internal static Color ControlPressed => Color.FromArgb(IsDark ? "#2E4350" : "#B7C7D0");
        internal static Color Warning => Color.FromArgb(IsDark ? "#DFA45C" : "#C97816");
        internal static Color Danger => Color.FromArgb(IsDark ? "#EA7A72" : "#C93919");
        internal static Color Success => Color.FromArgb(IsDark ? "#72C28A" : "#2E7D32");
    }
}
