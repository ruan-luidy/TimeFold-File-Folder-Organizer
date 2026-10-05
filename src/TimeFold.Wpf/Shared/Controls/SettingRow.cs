using System.Windows;
using System.Windows.Controls;

namespace TimeFold.Wpf.Shared.Controls
{
    /// <summary>
    /// A setting: label (and an optional description under it) on the left, the control on the right (from LookAway).
    /// <code>&lt;controls:SettingRow Label="Generate CSV log"&gt;&lt;ToggleButton ... /&gt;&lt;/controls:SettingRow&gt;</code>
    /// </summary>
    public class SettingRow : ContentControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(SettingRow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingRow), new PropertyMetadata(string.Empty));

        static SettingRow()
        {
            FocusableProperty.OverrideMetadata(typeof(SettingRow), new FrameworkPropertyMetadata(false));
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
    }
}
