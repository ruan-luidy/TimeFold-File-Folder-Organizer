using System.Windows;
using System.Windows.Media;
using HandyControl.Themes;

namespace TimeFold.Wpf.Shared.Services
{
    /// <summary>
    /// Light or dark, as in Axiom: HandyControl swaps its palette under the controls and the few brushes
    /// of our own are replaced here.
    /// </summary>
    public static class ThemeService
    {
        public static bool IsDark { get; private set; } = true;

        public static void Apply(bool dark)
        {
            IsDark = dark;
            if (Application.Current is null)
                return;

            ThemeManager.Current.ApplicationTheme = dark ? ApplicationTheme.Dark : ApplicationTheme.Light;

            var resources = Application.Current.Resources;
            resources["Workspace.BackgroundBrush"] = Frozen(dark ? Color.FromRgb(0x11, 0x11, 0x13) : Color.FromRgb(0xF3, 0xF3, 0xF5));
            resources["Hover.RowBrush"] = Frozen(dark ? Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x0D, 0x00, 0x00, 0x00));
            resources["Target.Brush"] = Frozen(dark ? Color.FromRgb(0x5B, 0x9B, 0xE0) : Color.FromRgb(0x2B, 0x63, 0xA3));
            resources["Target.CustomBrush"] = Frozen(dark ? Color.FromRgb(0xC0, 0x84, 0xFC) : Color.FromRgb(0x93, 0x33, 0xEA));
        }

        private static SolidColorBrush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
