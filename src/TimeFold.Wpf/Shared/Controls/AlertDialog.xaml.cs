using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using MahApps.Metro.IconPacks;
using TimeFold.Wpf.Shared.Icons;

namespace TimeFold.Wpf.Shared.Controls
{
    public enum DialogKind
    {
        Info,
        Success,
        Warning,
        Error,
    }

    public partial class AlertDialog : DialogCard
    {
        private readonly string? _details;

        public AlertDialog(DialogKind kind, string title, string message, string primaryText, string? secondaryText = null, string? details = null, string? optionText = null)
        {
            InitializeComponent();
            CancelResult = false;

            TitleText.Text = title;
            MessageText.Text = message;
            PrimaryButton.Content = primaryText;
            _details = string.IsNullOrWhiteSpace(details) ? null : details;
            DetailsLink.Visibility = _details is null ? Visibility.Collapsed : Visibility.Visible;

            if (secondaryText is not null)
            {
                SecondaryButton.Content = secondaryText;
                SecondaryButton.Visibility = Visibility.Visible;
            }

            if (optionText is not null)
            {
                OptionCheck.Content = optionText;
                OptionCheck.Visibility = Visibility.Visible;
            }

            var (icon, brush) = kind switch
            {
                DialogKind.Success => (PackIconPhosphorIconsKind.CheckCircleFill, "SuccessBrush"),
                DialogKind.Warning => (PackIconPhosphorIconsKind.WarningFill, "WarningBrush"),
                DialogKind.Error => (PackIconPhosphorIconsKind.XCircleFill, "DangerBrush"),
                _ => (PackIconPhosphorIconsKind.InfoFill, "PrimaryBrush"),
            };
            HeaderIcon.Data = PhosphorExtension.Get(icon);
            HeaderIcon.SetResourceReference(Shape.FillProperty, brush);

            // focus the main button once the overlay is up, so Enter works right away
            Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => Keyboard.Focus(PrimaryButton));
        }

        public bool IsOptionChecked => OptionCheck.IsChecked == true;

        private void DetailsLink_Click(object sender, RoutedEventArgs e)
        {
            var open = DetailsBox.Visibility != Visibility.Visible;
            DetailsBox.Text = _details;
            DetailsBox.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            DetailsLinkText.Text = open ? "Hide details" : "Show details";
            Width = open ? 560 : 400;
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e) => Finish(true);

        private void SecondaryButton_Click(object sender, RoutedEventArgs e) => Finish(false);
    }
}
