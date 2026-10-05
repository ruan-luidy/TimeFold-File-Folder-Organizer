using System.Globalization;
using System.IO;
using System.Windows;
using TimeFold.Core.Undo;
using TimeFold.Wpf.Shared.Controls;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Undo
{
    public enum UndoAction
    {
        Cancel,
        Start,
        Discard,
    }

    public readonly record struct UndoChoice(UndoAction Action, string? DedicatedFolder);

    // The UndoConfirmDialog: preflight numbers and where to restore (original places or a Restored_ folder)
    public partial class UndoDialog : DialogCard
    {
        private readonly string _safeFolderPath;

        public UndoDialog(UndoSession session, UndoPreflightReport preflight)
        {
            InitializeComponent();
            CancelResult = new UndoChoice(UndoAction.Cancel, null);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_hh-mmtt");
            _safeFolderPath = Path.Combine(session.SourceDirectory, $"Restored_{timestamp}");

            TimeSpan age = DateTime.Now - session.Timestamp;
            string ageDesc = age.TotalDays >= 1 ? $"{(int)age.TotalDays}d ago" : (age.TotalHours >= 1 ? $"{(int)age.TotalHours}h ago" : "just now");
            bool isHighMissing = preflight.TotalItems > 0 && ((double)preflight.MissingAtDestination / preflight.TotalItems) >= 0.8;

            ReadyText.Text = $"{preflight.ReadyToRestore} / {preflight.TotalItems}";
            MissingText.Text = $"{preflight.MissingAtDestination}";
            CollisionsText.Text = $"{preflight.OriginalPathCollisions}";
            OrganizedText.Text = $"Organized {session.Timestamp.ToString("MMM dd, yyyy, h:mm tt", CultureInfo.InvariantCulture)} ({ageDesc}) from:";
            SourceText.Text = session.SourceDirectory;
            DedicatedPathText.Text = _safeFolderPath;

            string staleNote = preflight.ReadyToRestore == 0
                ? "All organized files were moved or deleted outside TimeFold."
                : (isHighMissing ? "Most files were moved or deleted since organization." : "");
            if (preflight.MissingAtDestination > 0) staleNote = (staleNote + $" Missing items ({preflight.MissingAtDestination}) will be skipped.").Trim();
            if (preflight.OriginalPathCollisions > 0) staleNote = (staleNote + $" Collisions at source ({preflight.OriginalPathCollisions}) will get '(Restored)' appended.").Trim();
            if (staleNote.Length > 0)
            {
                StaleText.Text = staleNote;
                StaleText.Visibility = Visibility.Visible;
            }

            if (preflight.ReadyToRestore == 0)
            {
                StartButton.IsEnabled = false;
                StartButton.Content = "Nothing to Restore";
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e) =>
            Finish(new UndoChoice(UndoAction.Start, DedicatedOption.IsChecked == true ? _safeFolderPath : null));

        private async void Discard_Click(object sender, RoutedEventArgs e)
        {
            // the dialogs stack: the confirmation opens over this one
            bool discard = await DialogService.ConfirmAsync("Discard Undo History", "Permanently discard this undo session history?\n\nThis cannot be undone.", "Discard");
            if (discard) Finish(new UndoChoice(UndoAction.Discard, null));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Finish(new UndoChoice(UndoAction.Cancel, null));
    }
}
