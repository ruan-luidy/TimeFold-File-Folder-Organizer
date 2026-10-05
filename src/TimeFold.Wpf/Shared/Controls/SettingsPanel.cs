using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimeFold.Wpf.Shared.Controls
{
    /// <summary>A card of a settings page: icon and title on top, a line on what it is for, then its rows (from LookAway).</summary>
    public class SettingsPanel : HeaderedContentControl
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(SettingsPanel));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsPanel), new PropertyMetadata(string.Empty));

        static SettingsPanel()
        {
            FocusableProperty.OverrideMetadata(typeof(SettingsPanel), new FrameworkPropertyMetadata(false));
        }

        public Geometry Icon
        {
            get => (Geometry)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
    }
}
