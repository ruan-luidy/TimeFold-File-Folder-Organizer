using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TimeFold.Wpf.Shared.Controls;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.FileTypes
{
    public readonly record struct CategoryChoice(string? Category, bool Reset);

    // "Set target folder for all .ext files": pick a category or type a new folder name (ShowCategoryPickerPrompt)
    public partial class CategoryPickerDialog : DialogCard
    {
        private readonly List<string> _categories;
        private bool _filling;

        private CategoryPickerDialog(string ext, string currentCategory, List<string> categories, bool hasCustomOverride)
        {
            InitializeComponent();
            CancelResult = new CategoryChoice(null, false);
            _categories = categories;

            TitleText.Text = $"Set Target Folder for {ext}";
            DescriptionText.Text = $"All '{ext}' items are currently routed to: {currentCategory}\nSelect an existing category or type a custom destination folder:";
            ResetButton.Visibility = hasCustomOverride ? Visibility.Visible : Visibility.Collapsed;
            Categories.ItemsSource = categories;
            Input.Text = currentCategory;

            Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                Keyboard.Focus(Input);
                Input.SelectAll();
            });
        }

        public static async Task<CategoryChoice> ShowAsync(string ext, string currentCategory, List<string> categories, bool hasCustomOverride) =>
            await DialogService.ShowAsync<CategoryChoice>(new CategoryPickerDialog(ext, currentCategory, categories, hasCustomOverride));

        private void Apply()
        {
            string text = Input.Text.Trim();
            string? error = string.IsNullOrWhiteSpace(text) ? "Please enter a destination folder name."
                : text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ? "Folder name cannot contain any of the following characters:\n\\ / : * ? \" < > |"
                : null;
            if (error != null)
            {
                ErrorText.Text = error;
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            Finish(new CategoryChoice(text, false));
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            if (_filling) return;
            string q = Input.Text.Trim();
            Categories.ItemsSource = q.Length == 0 || _categories.Any(c => c.Equals(q, StringComparison.OrdinalIgnoreCase))
                ? _categories
                : _categories.Where(c => c.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void Categories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Categories.SelectedItem is not string category) return;
            _filling = true;
            Input.Text = category;
            _filling = false;
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Apply();
                e.Handled = true;
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e) => Apply();

        private void Reset_Click(object sender, RoutedEventArgs e) => Finish(new CategoryChoice(null, true));

        private void Cancel_Click(object sender, RoutedEventArgs e) => Finish(new CategoryChoice(null, false));
    }
}
