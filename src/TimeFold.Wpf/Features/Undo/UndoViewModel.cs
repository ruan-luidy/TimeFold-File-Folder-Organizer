using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeFold.Core.Undo;
using TimeFold.Wpf.Features.Source;
using TimeFold.Wpf.Shared.Progress;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Undo
{
    /// <summary>Undo Last Organization (Beta): the File menu entry, Ctrl+Z and the button on the completion screen.</summary>
    public sealed partial class UndoViewModel : ObservableObject
    {
        private readonly ProgressViewModel _progress;
        private readonly SourceViewModel _source;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
        private bool _hasSession;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
        private bool _isRunning;

        public UndoViewModel(ProgressViewModel progress, SourceViewModel source)
        {
            _progress = progress;
            _source = source;
            _progress.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ProgressViewModel.IsActive)) UndoCommand.NotifyCanExecuteChanged();
            };
            RefreshState();
        }

        /// <summary>Raised when an undo really starts moving files back.</summary>
        public event EventHandler? Started;

        public void RefreshState() => HasSession = UndoService.HasActiveSession();

        private bool CanUndo() => HasSession && !IsRunning && !_progress.IsActive;

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private async Task Undo()
        {
            if (IsRunning || _progress.IsActive) return;

            var session = UndoService.LoadSession();
            if (session == null || session.MovedItems.Count == 0)
            {
                await DialogService.InfoAsync("Undo (Beta)", "No previous organization run found to undo.");
                RefreshState();
                return;
            }

            var preflight = UndoService.PreflightCheck(session);
            if (!preflight.CanProceed)
            {
                bool clear = await DialogService.ConfirmAsync(
                    "No Files Found to Restore",
                    "Cannot perform undo:\n\nNone of the organized files were found at their destination folders.\nThey may have already been moved, renamed, or deleted outside TimeFold.\n\nWould you like to clear this undo history?",
                    "Clear History",
                    "Keep",
                    Shared.Controls.DialogKind.Info);
                if (clear)
                {
                    UndoService.ClearSession();
                    RefreshState();
                }
                return;
            }

            var choice = await DialogService.ShowAsync<UndoChoice>(new UndoDialog(session, preflight));
            if (choice.Action == UndoAction.Discard)
            {
                UndoService.ClearSession();
                RefreshState();
                return;
            }
            if (choice.Action != UndoAction.Start) return;

            string? customRestoreDir = choice.DedicatedFolder;
            string restoreTargetDesc = customRestoreDir != null
                ? $"Safe Folder ({customRestoreDir})"
                : $"Original Locations ({session.SourceDirectory})";

            IsRunning = true;
            Started?.Invoke(this, EventArgs.Empty);
            var (progress, token) = _progress.Begin(
                "Restoring Files...",
                "Restoring",
                "↩",
                "Cancel the undo? Files already restored stay where they are.",
                "Starting Undo Operation (Beta)...",
                $"Restoring files to: {restoreTargetDesc}",
                string.Empty);

            try
            {
                var result = await UndoService.UndoAsync(session, progress, token, customRestoreDir);
                _progress.End();

                var summary = new StringBuilder();
                summary.AppendLine($"Restored: {result.RestoredCount} file(s)");
                summary.AppendLine($"Cleaned up: {result.PrunedFoldersCount} empty folder(s)");
                if (result.CollisionRenamedCount > 0)
                    summary.AppendLine($"Auto-renamed: {result.CollisionRenamedCount} item(s) preserved with '(Restored)'");
                if (result.FailureCount > 0)
                    summary.AppendLine($"Skipped/error: {result.FailureCount} item(s)");

                await DialogService.AlertAsync(
                    result.FailureCount > 0 ? Shared.Controls.DialogKind.Warning : Shared.Controls.DialogKind.Success,
                    "Undo Complete",
                    summary.ToString().TrimEnd(),
                    result.ErrorMessages.Count > 0 ? string.Join(Environment.NewLine, result.ErrorMessages) : null);

                string folderToView = customRestoreDir != null && Directory.Exists(customRestoreDir)
                    ? customRestoreDir
                    : session.SourceDirectory;
                _source.SetSourceFolder(folderToView);
            }
            catch (OperationCanceledException)
            {
                _progress.End();
                await DialogService.InfoAsync("Cancelled", "Undo operation was cancelled.");
            }
            catch (Exception ex)
            {
                _progress.End();
                await DialogService.ErrorAsync("Undo Error", $"Error during undo: {ex.Message}", ex.ToString());
            }
            finally
            {
                _progress.End();
                IsRunning = false;
                RefreshState();
            }
        }
    }
}
