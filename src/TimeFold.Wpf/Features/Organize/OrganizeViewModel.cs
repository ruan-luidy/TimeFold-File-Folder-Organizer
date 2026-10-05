using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeFold.Core.Config;
using TimeFold.Core.Conflicts;
using TimeFold.Core.Organizing;
using TimeFold.Core.Undo;
using TimeFold.Wpf.Features.Conflicts;
using TimeFold.Wpf.Features.Preview;
using TimeFold.Wpf.Features.Source;
using TimeFold.Wpf.Features.Undo;
using TimeFold.Wpf.Shared.Controls;
using TimeFold.Wpf.Shared.Progress;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Organize
{
    /// <summary>
    /// Start Organization and the screen after it (BtnStart_Click, ShowCompletion and the completion panel of MainForm).
    /// </summary>
    public sealed partial class OrganizeViewModel : ObservableObject
    {
        private readonly AppState _state;
        private readonly PreviewViewModel _preview;
        private readonly SourceViewModel _source;
        private readonly ProgressViewModel _progress;
        private readonly UndoViewModel _undo;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCommand))]
        private bool _isRunning;

        [ObservableProperty]
        private OrganizationResult? _result;

        public OrganizeViewModel(AppState state, PreviewViewModel preview, SourceViewModel source, ProgressViewModel progress, UndoViewModel undo)
        {
            _state = state;
            _preview = preview;
            _source = source;
            _progress = progress;
            _undo = undo;

            _preview.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PreviewViewModel.CanStart)) StartCommand.NotifyCanExecuteChanged();
            };
            // an undo started from the completion screen goes back to the plan
            _undo.Started += (_, _) => Result = null;
        }

        public UndoViewModel Undo => _undo;

        public bool IsComplete => Result != null;

        public string StartLabel => _state.Settings.OrgMode switch
        {
            Core.Settings.OrganizationMode.Date => "Move files & folders into their date-based timeline folders",
            _ => "Move files & folders into their target folders",
        };

        public string FoldersCreatedText => Result is null ? string.Empty : $"{Result.MonthFoldersCreated}";

        public string MovedText => Result is null ? string.Empty : $"{Result.FilesMoved:N0}";

        public string MovedCaption => Result is null ? string.Empty : $"of {Result.TotalFiles:N0} organized";

        public bool HasRenamed => Result?.ConflictsResolved > 0;

        public bool HasErrors => Result?.Errors > 0;

        public string ErrorsDetails => Result is null ? string.Empty : string.Join(Environment.NewLine, Result.ErrorMessages);

        public string OutputPath => string.IsNullOrWhiteSpace(Result?.SortedFolderPath) ? "Not available" : Result!.SortedFolderPath;

        public bool HasCsvLog => !string.IsNullOrWhiteSpace(Result?.CsvLogPath);

        partial void OnResultChanged(OrganizationResult? value)
        {
            OnPropertyChanged(nameof(IsComplete));
            OnPropertyChanged(nameof(FoldersCreatedText));
            OnPropertyChanged(nameof(MovedText));
            OnPropertyChanged(nameof(MovedCaption));
            OnPropertyChanged(nameof(HasRenamed));
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(ErrorsDetails));
            OnPropertyChanged(nameof(OutputPath));
            OnPropertyChanged(nameof(HasCsvLog));
            OpenOutputCommand.NotifyCanExecuteChanged();
        }

        private bool CanStart() => !IsRunning && _preview.CanStart;

        [RelayCommand(CanExecute = nameof(CanStart))]
        private async Task Start()
        {
            if (IsRunning || _preview.Service is not { } service) return;
            var settings = _state.Settings;

            if (string.IsNullOrEmpty(_state.SourceFolder) || !Directory.Exists(_state.SourceFolder))
            {
                await DialogService.WarningAsync("Invalid Source", "Please select a valid source folder.");
                return;
            }

            string outputDir = _state.OutputBase;
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                await DialogService.WarningAsync("Invalid Output", "Please select a valid output folder.");
                return;
            }

            if (!await CheckWritePermissionAsync(outputDir)) return;

            var files = _preview.Files.ToList();
            if (files.Count == 0)
            {
                await DialogService.InfoAsync("No Files Loaded", "Please select a folder to preview files first.");
                return;
            }

            var itemsToOrganize = files.Where(f => f.IsSelected && !f.IsExcludedByRule).ToList();
            if (itemsToOrganize.Count == 0)
            {
                await DialogService.InfoAsync("No Items Selected", "No items are selected to organize. Please check at least one item in the preview list.");
                return;
            }

            var conflictStrategy = ConflictResolutionStrategy.AutoRename;
            if (_preview.Conflicts.Count > 0)
            {
                var chosen = await DialogService.ShowAsync<ConflictResolutionStrategy?>(new ConflictDialog(_preview.Conflicts));
                if (chosen is not { } strategy) return;
                conflictStrategy = strategy;
            }

            string warningExtra = _preview.HasTimestampWarning
                ? "\n\nWarning: Almost all items share identical or similar timestamps (common with downloaded ZIPs or chat media) and will be organized into a single folder."
                : "";
            string previewOutputDir = settings.CreateSortedSubfolder
                ? AppConstants.GetSortedFolderPreviewPath(outputDir, settings.Use24HourTimestamp)
                : outputDir;
            string destDesc = settings.CreateSortedSubfolder ? "a Sorted folder in the output location" : "the output location directly";
            string skipNotice = files.Count > itemsToOrganize.Count
                ? $"\n({files.Count - itemsToOrganize.Count:N0} unchecked/excluded item(s) will remain untouched at source)\n"
                : "";

            bool confirmed = await DialogService.ConfirmAsync(
                "Confirm Organization",
                $"You are about to organize {itemsToOrganize.Count:N0} selected item(s).\n" +
                skipNotice +
                $"\nSource: {_state.SourceFolder}\n" +
                $"Output: {previewOutputDir}" +
                warningExtra +
                $"\n\nThis will move files from the source location to {destDesc}.",
                "Start Organizing",
                kind: _preview.HasTimestampWarning ? DialogKind.Warning : DialogKind.Info);
            if (!confirmed) return;

            IsRunning = true;
            var (progress, token) = _progress.Begin(
                "Organizing Files...",
                "Processing",
                "✓",
                "Are you sure you want to cancel? Partial progress will be saved.",
                "Starting organization...",
                $"Source: {service.WorkingDirectory}",
                $"Output: {service.OutputDirectory}",
                string.Empty);

            try
            {
                var collidingPaths = new HashSet<string>(_preview.Conflicts.Select(c => c.Item.FullPath), StringComparer.OrdinalIgnoreCase);
                var orgResult = await service.OrganizeFilesAsync(
                    files, progress, token,
                    settings.GenerateCsvLog, conflictStrategy, collidingPaths);

                RecordUndoSession(service, files, orgResult);
                Result = orgResult;
            }
            catch (OperationCanceledException)
            {
                if (service.LastResult != null && service.LastResult.FilesMoved > 0)
                {
                    RecordUndoSession(service, files, service.LastResult);
                }
                _progress.End();
                await DialogService.InfoAsync("Cancelled", "Organization was cancelled. Any processed files have been logged.");
                await _preview.LoadAsync();
            }
            catch (Exception ex)
            {
                _progress.End();
                await DialogService.ErrorAsync("Error", $"Error during organization: {ex.Message}", ex.ToString());
            }
            finally
            {
                _progress.End();
                IsRunning = false;
                _undo.RefreshState();
            }
        }

        private void RecordUndoSession(FileOrganizerService service, List<Core.Files.FileItem> files, OrganizationResult result)
        {
            UndoService.RecordSession(
                service.WorkingDirectory,
                service.OutputDirectory,
                result.SortedFolderPath,
                files,
                null,
                result.CsvLogPath);
            _undo.RefreshState();
        }

        /// <summary>The probe of AppConstants.CheckDirectoryWritePermission; the dialogs now live here.</summary>
        private static async Task<bool> CheckWritePermissionAsync(string targetDir)
        {
            if (AppConstants.TryWriteProbe(targetDir, out var error)) return true;

            if (error is UnauthorizedAccessException)
            {
                bool openSecurity = await DialogService.ConfirmAsync(
                    "Write Permission Denied",
                    $"Windows denied write access to folder:\n{targetDir}\n\n" +
                    "This usually occurs when Windows Controlled Folder Access (Ransomware Protection) protects personal folders (Documents, Desktop, etc.).\n\n" +
                    "Would you like to open Windows Security to allow TimeFold?",
                    "Open Windows Security",
                    "Not now");
                if (openSecurity)
                {
                    try { ShellActions.Open("windowsdefender://ransomwareprotection"); }
                    catch { ShellActions.OpenUrl("ms-settings:windowsdefender"); }
                }
                return false;
            }

            await DialogService.ErrorAsync("Permission Error", $"Unable to write to destination folder:\n{error?.Message}");
            return false;
        }

        private bool CanOpenOutput() => Result != null;

        [RelayCommand(CanExecute = nameof(CanOpenOutput))]
        private async Task OpenOutput()
        {
            var targetPath = Result?.SortedFolderPath;
            if (!string.IsNullOrWhiteSpace(targetPath) && Directory.Exists(targetPath)) { ShellActions.OpenFolder(targetPath); return; }
            var fallback = _preview.Service?.OutputDirectory;
            if (!string.IsNullOrWhiteSpace(fallback) && Directory.Exists(fallback)) { ShellActions.OpenFolder(fallback); return; }
            await DialogService.InfoAsync("Folder Unavailable", "Output folder is not available yet.");
        }

        [RelayCommand]
        private void OpenCsvLog()
        {
            if (HasCsvLog) ShellActions.Reveal(Result!.CsvLogPath);
        }

        [RelayCommand]
        private void ShowErrors() => _ = DialogService.ErrorAsync("Errors Encountered", $"{Result?.Errors} item(s) could not be moved.", ErrorsDetails);

        /// <summary>"Organize Another Folder" and File > New.</summary>
        [RelayCommand]
        public void StartNew()
        {
            if (IsRunning) return;
            Result = null;
            _source.Reset();
            _preview.Reset();
        }

        [RelayCommand]
        private void Exit() => Application.Current.MainWindow?.Close();

        public void RefreshLabels() => OnPropertyChanged(nameof(StartLabel));
    }
}
