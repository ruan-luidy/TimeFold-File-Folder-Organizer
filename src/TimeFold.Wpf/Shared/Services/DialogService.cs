using System.Windows;
using HandyControl.Controls;
using TimeFold.Wpf.Shared.Controls;

namespace TimeFold.Wpf.Shared.Services
{
    /// <summary>
    /// Dialogs as in the Terminal: a card over the window with the background dimmed (HandyControl's Dialog),
    /// never a new window or the raw MessageBox. Without the main window on screen it falls back to MessageBox.
    /// </summary>
    public static class DialogService
    {
        public static async Task<T?> ShowAsync<T>(DialogCard card)
        {
            var dialog = Dialog.Show(card);
            try
            {
                return await card.Result is T value ? value : default;
            }
            finally
            {
                dialog.Close();
            }
        }

        public static Task InfoAsync(string title, string message, string? details = null) =>
            AlertAsync(DialogKind.Info, title, message, details);

        public static Task SuccessAsync(string title, string message) =>
            AlertAsync(DialogKind.Success, title, message);

        public static Task WarningAsync(string title, string message) =>
            AlertAsync(DialogKind.Warning, title, message);

        public static Task ErrorAsync(string title, string message, string? details = null) =>
            AlertAsync(DialogKind.Error, title, message, details);

        public static async Task AlertAsync(DialogKind kind, string title, string message, string? details = null)
        {
            if (!CanShowOverlay())
            {
                var icon = kind switch
                {
                    DialogKind.Error => MessageBoxImage.Error,
                    DialogKind.Warning => MessageBoxImage.Warning,
                    _ => MessageBoxImage.Information,
                };
                System.Windows.MessageBox.Show(details is null ? message : $"{message}\n\n{details}", title, MessageBoxButton.OK, icon);
                return;
            }

            await ShowAsync<bool>(new AlertDialog(kind, title, message, "OK", details: details));
        }

        /// <summary>True on the primary button, false on the secondary or Esc.</summary>
        public static async Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText = "Cancel", DialogKind kind = DialogKind.Warning)
        {
            if (!CanShowOverlay())
                return System.Windows.MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;

            return await ShowAsync<bool>(new AlertDialog(kind, title, message, confirmText, cancelText));
        }

        /// <summary>Text field dialog; null when cancelled. <paramref name="validate"/> returns an error to show, or null.</summary>
        public static Task<string?> PromptAsync(string title, string label, string text, string confirmText = "Save", bool selectNameOnly = false, Func<string, string?>? validate = null) =>
            ShowAsync<string>(new TextPromptDialog(title, label, text, confirmText, selectNameOnly, validate));

        private static bool CanShowOverlay() =>
            Application.Current?.MainWindow is { IsLoaded: true, IsVisible: true };
    }
}
