using System.Windows;
using TimeFold.Core.Conflicts;
using TimeFold.Wpf.Shared.Controls;

namespace TimeFold.Wpf.Features.Conflicts
{
    /// <summary>
    /// The collisions before a run. Finishes with the chosen ConflictResolutionStrategy, or null on Cancel.
    /// In review mode (the banner's "Review") it only lists them.
    /// </summary>
    public partial class ConflictDialog : DialogCard
    {
        public ConflictDialog(IReadOnlyList<ConflictInfo> conflicts, bool reviewOnly = false)
        {
            InitializeComponent();
            TitleText.Text = $"{conflicts.Count} Collision / Conflict{(conflicts.Count == 1 ? "" : "s")} Detected";
            Table.ItemsSource = conflicts;

            if (reviewOnly)
            {
                SkipButton.Visibility = Visibility.Collapsed;
                RenameButton.Content = "Close";
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Rename_Click(object sender, RoutedEventArgs e) => Finish(ConflictResolutionStrategy.AutoRename);

        private void Skip_Click(object sender, RoutedEventArgs e) => Finish(ConflictResolutionStrategy.Skip);

        private void Cancel_Click(object sender, RoutedEventArgs e) => Finish(null);
    }
}
