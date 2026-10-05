using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TimeFold.Wpf.Shared.Controls
{
    // One text field (rename file, rename category). The error shows under the field instead of a second dialog.
    public partial class TextPromptDialog : DialogCard
    {
        private readonly Func<string, string?>? _validate;

        public TextPromptDialog(string title, string label, string text, string confirmText, bool selectNameOnly, Func<string, string?>? validate)
        {
            InitializeComponent();
            _validate = validate;

            TitleText.Text = title;
            LabelText.Text = label;
            ConfirmButton.Content = confirmText;
            Input.Text = text;

            Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                Keyboard.Focus(Input);
                // name without the extension selected, as in Explorer
                int dot = text.LastIndexOf('.');
                if (selectNameOnly && dot > 0) Input.Select(0, dot);
                else Input.SelectAll();
            });
        }

        private void Confirm()
        {
            string text = Input.Text.Trim();
            if (_validate?.Invoke(text) is { } error)
            {
                ErrorText.Text = error;
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            Finish(text);
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Confirm();
                e.Handled = true;
            }
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e) => ErrorText.Visibility = Visibility.Collapsed;

        private void Confirm_Click(object sender, RoutedEventArgs e) => Confirm();

        private void Cancel_Click(object sender, RoutedEventArgs e) => Finish(null);
    }
}
