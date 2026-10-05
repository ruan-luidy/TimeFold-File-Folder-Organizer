using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TimeFold.Wpf.Shared.Controls
{
    public partial class EmptyState : UserControl
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(Geometry), typeof(EmptyState));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(EmptyState));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(EmptyState));

        public static readonly DependencyProperty ActionTextProperty = DependencyProperty.Register(
            nameof(ActionText), typeof(string), typeof(EmptyState));

        public static readonly DependencyProperty ActionCommandProperty = DependencyProperty.Register(
            nameof(ActionCommand), typeof(ICommand), typeof(EmptyState));

        public EmptyState()
        {
            InitializeComponent();
        }

        public Geometry? Icon
        {
            get => (Geometry?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string? Text
        {
            get => (string?)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string? ActionText
        {
            get => (string?)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public ICommand? ActionCommand
        {
            get => (ICommand?)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }
    }
}
