using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TimeFold.Core.Config;
using TimeFold.Core.Conflicts;
using TimeFold.Core.Exclusions;
using TimeFold.Core.Files;
using TimeFold.Core.FileTypes;
using TimeFold.Core.Organizing;
using TimeFold.Core.Settings;
using TimeFold.Wpf.Features.Conflicts;
using TimeFold.Wpf.Features.FileTypes;
using TimeFold.Wpf.Shared.Controls;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Preview
{
    public enum SummaryTone
    {
        Muted,
        Warning,
        Ready,
    }

    /// <summary>Asks the shell to open a settings page ("Naming", "FileTypes", "Exclusions"...).</summary>
    public sealed record OpenSettingsMessage(string Page);

    /// <summary>Asks the shell for the folder picker (the empty preview's button).</summary>
    public sealed record BrowseMessage;

    /// <summary>
    /// The live preview: scans the source folder, keeps the plan (FileItems), the selection, the conflicts and the
    /// notices above the table. This is the part of MainForm (Events, Selections, OrgModes, ContextMenu) that was
    /// about the list.
    /// </summary>
    public sealed partial class PreviewViewModel : ObservableObject
    {
        private readonly AppState _state;
        private readonly StatusService _status;
        private readonly string _executablePath;
        private List<FileItem> _files = new();
        private List<ConflictInfo> _conflicts = new();
        private Dictionary<FileItem, ConflictInfo> _conflictByItem = new();
        private int _previewGeneration;
        private int _currentPreviewLimit;
        private int _sortColumn = 2;
        private bool _sortAscending;

        [ObservableProperty]
        private ObservableCollection<PreviewRow> _rows = new();

        [ObservableProperty]
        private ICollectionView? _rowsView;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private bool _hasMediaDateColumn;

        [ObservableProperty]
        private string _countText = string.Empty;

        [ObservableProperty]
        private string _ignoredText = string.Empty;

        [ObservableProperty]
        private int _remainingCount;

        [ObservableProperty]
        private string _conflictText = string.Empty;

        [ObservableProperty]
        private string _timestampWarning = string.Empty;

        [ObservableProperty]
        private string _exclusionsPausedText = string.Empty;

        [ObservableProperty]
        private string _summaryText = "Select a source folder to preview files.";

        [ObservableProperty]
        private SummaryTone _summaryTone = SummaryTone.Muted;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OpenSelectedCommand), nameof(RevealSelectedCommand), nameof(CopySelectedPathCommand), nameof(RenameSelectedCommand), nameof(DeleteSelectedCommand))]
        [NotifyPropertyChangedFor(nameof(Menu))]
        private PreviewRow? _selectedRow;

        [ObservableProperty]
        private string _filterText = string.Empty;

        public PreviewViewModel(AppState state, StatusService status)
        {
            _state = state;
            _status = status;
            _executablePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "TimeFold.exe");
            _state.PropertyChanged += OnStateChanged;
        }

        public AppSettings Settings => _state.Settings;

        public string OutputBase => _state.OutputBase;

        public FileOrganizerService? Service { get; private set; }

        public IReadOnlyList<FileItem> Files => _files;

        public IReadOnlyList<ConflictInfo> Conflicts => _conflicts;

        public bool HasFiles => _files.Count > 0;

        public bool HasSource => Service != null;

        public bool HasConflicts => _conflicts.Count > 0;

        public bool HasTimestampWarning => TimestampWarning.Length > 0;

        public bool HasExclusionsPaused => ExclusionsPausedText.Length > 0;

        public bool HasIgnored => IgnoredText.Length > 0;

        public bool CanStart => SummaryTone == SummaryTone.Ready;

        public bool HasMore => RemainingCount > 0;

        public string LoadMoreText => $"Load 1,000 more ({RemainingCount:N0} remaining)";

        public PreviewMenu Menu => new(SelectedRow, Settings);

        public ConflictInfo? ConflictFor(FileItem item) => _conflictByItem.GetValueOrDefault(item);

        partial void OnFilterTextChanged(string value) => RowsView?.Refresh();

        partial void OnRemainingCountChanged(int value)
        {
            OnPropertyChanged(nameof(LoadMoreText));
            OnPropertyChanged(nameof(HasMore));
        }

        partial void OnSummaryToneChanged(SummaryTone value) => OnPropertyChanged(nameof(CanStart));

        partial void OnTimestampWarningChanged(string value) => OnPropertyChanged(nameof(HasTimestampWarning));

        partial void OnExclusionsPausedTextChanged(string value) => OnPropertyChanged(nameof(HasExclusionsPaused));

        partial void OnIgnoredTextChanged(string value) => OnPropertyChanged(nameof(HasIgnored));

        partial void OnConflictTextChanged(string value) => OnPropertyChanged(nameof(HasConflicts));

        private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SourceFolder))
            {
                OpenSource(_state.SourceFolder);
            }
            else if (e.PropertyName == nameof(AppState.OutputBase))
            {
                if (Service != null && Directory.Exists(OutputBase)) Service.OutputDirectory = OutputBase;
                CheckConflicts();
                RefreshRows();
            }
        }

        private void OpenSource(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                Reset();
                return;
            }

            Service = new FileOrganizerService(_executablePath, folder);
            ApplyNamingSettings();
            if (Directory.Exists(OutputBase)) Service.OutputDirectory = OutputBase;
            OnPropertyChanged(nameof(HasSource));
            _ = LoadAsync();
        }

        public void ApplyNamingSettings()
        {
            Service?.ApplyNamingSettings(
                Settings.FolderFormat, Settings.FolderPrefix, Settings.FolderSuffix,
                Settings.Use24HourTimestamp, Settings.OrgMode,
                Settings.KeepHtmlCompanionsTogether, Settings.CategoryPrefix,
                Settings.CategorySuffix, Settings.KeepSubtitleCompanionsTogether,
                Settings.CreateSortedSubfolder, Settings.GroupGitRepositories,
                Settings.PackageVideoSubtitles);
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (Service == null) return;
            var service = Service;
            int currentGen = ++_previewGeneration;
            IsScanning = true;
            try
            {
                _currentPreviewLimit = Settings.MaxPreviewItems > 0 ? Settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems;
                var files = await Task.Run(() =>
                    service.ScanFiles(Settings.IncludeTopLevelFolders, Settings.IgnoreSystemFiles, Settings.FileDateSource, Settings.FolderDateSource, null, Settings.EnableFolderExclusions, Settings.UseMediaDateTaken));
                if (currentGen != _previewGeneration) return;
                _files = files;
                _sortColumn = 2;
                _sortAscending = false;
                CheckConflicts();
                BuildRows();
                UpdateSummary();
                CheckTimestampSimilarity();
                CheckExclusionsPausedWarning();
                OnPropertyChanged(nameof(HasFiles));
                SortChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                await DialogService.ErrorAsync("Error", $"Error loading files: {ex.Message}");
            }
            finally
            {
                if (currentGen == _previewGeneration) IsScanning = false;
            }
        }

        /// <summary>Raised when the sort column changes, so the grid can draw the arrow.</summary>
        public event EventHandler? SortChanged;

        public (int Column, bool Ascending) Sort => (_sortColumn, _sortAscending);

        public void Reset()
        {
            _previewGeneration++;
            Service = null;
            _files = new();
            _conflicts = new();
            _conflictByItem = new();
            _currentPreviewLimit = 0;
            Rows = new();
            RowsView = null;
            HasMediaDateColumn = false;
            RemainingCount = 0;
            ConflictText = TimestampWarning = ExclusionsPausedText = IgnoredText = string.Empty;
            CountText = string.Empty;
            SummaryTone = SummaryTone.Muted;
            SummaryText = "Select a source folder to preview files.";
            IsScanning = false;
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(HasSource));
        }

        private void BuildRows()
        {
            HasMediaDateColumn = Settings.UseMediaDateTaken && _files.Any(f => f.IsMediaDateActive && f.MediaDateTaken.HasValue);

            int maxPreview = _currentPreviewLimit > 0 ? _currentPreviewLimit : (Settings.MaxPreviewItems > 0 ? Settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
            var displayed = _files.OrderByDescending(f => f.IsExcludedByRule).Take(maxPreview).Select(f => new PreviewRow(f, this));

            Rows = new ObservableCollection<PreviewRow>(displayed);
            var view = CollectionViewSource.GetDefaultView(Rows);
            view.Filter = MatchesFilter;
            RowsView = view;
            RemainingCount = _files.Count - Rows.Count;
            UpdateHeader();
        }

        private bool MatchesFilter(object row)
        {
            if (string.IsNullOrWhiteSpace(FilterText) || row is not PreviewRow r) return true;
            string q = FilterText.Trim();
            return r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.Item.TargetFolder.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.TypeDisplay.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshRows()
        {
            foreach (var row in Rows) row.Refresh();
        }

        [RelayCommand]
        private void LoadMore()
        {
            int current = _currentPreviewLimit > 0 ? _currentPreviewLimit : (Settings.MaxPreviewItems > 0 ? Settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
            _currentPreviewLimit = current + 1000;
            BuildRows();
        }

        /// <summary>Column order of FileItemComparer: name, type, modified, created, [taken], target, status, size.</summary>
        public void SortBy(string key)
        {
            int column = key switch
            {
                "Name" => 0,
                "Type" => 1,
                "Modified" => 2,
                "Created" => 3,
                "Taken" => 4,
                "Target" => HasMediaDateColumn ? 5 : 4,
                "Status" => HasMediaDateColumn ? 6 : 5,
                "Size" => HasMediaDateColumn ? 7 : 6,
                _ => 2
            };

            if (_sortColumn == column) _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = column;
                bool isDesc = column == 2 || column == 3 || (HasMediaDateColumn ? (column == 4 || column == 6) : (column == 5));
                _sortAscending = !isDesc;
            }

            FileItemComparer.Sort(_files, _sortColumn, _sortAscending, HasMediaDateColumn);
            BuildRows();
            SortChanged?.Invoke(this, EventArgs.Empty);
        }

        public string SortKey(int column) => column switch
        {
            0 => "Name",
            1 => "Type",
            2 => "Modified",
            3 => "Created",
            _ when HasMediaDateColumn => column switch { 4 => "Taken", 5 => "Target", 6 => "Status", _ => "Size" },
            _ => column switch { 4 => "Target", 5 => "Status", _ => "Size" }
        };

        // ----- selection -----

        public async void SetChecked(PreviewRow row, bool value)
        {
            var file = row.Item;
            if (file.IsExcludedByRule)
            {
                row.Refresh();
                if (value)
                {
                    await DialogService.InfoAsync(
                        "Folder Excluded by Rule",
                        $"'{file.Name}' is excluded by your Folder Exclusion Rules.\n\nTo organize this folder, open Settings > Exclusions or right-click it and choose to stop ignoring it.");
                }
                return;
            }

            file.IsSelected = value;
            AfterSelectionChanged();
        }

        private void MutateSelection(Action<FileItem> mutate)
        {
            if (_files.Count == 0) return;
            foreach (var file in _files)
            {
                if (!file.IsExcludedByRule) mutate(file);
            }
            AfterSelectionChanged();
        }

        private void AfterSelectionChanged()
        {
            CheckConflicts();
            RefreshRows();
            UpdateSummary();
            UpdateHeader();
        }

        [RelayCommand]
        private void SelectAll() => MutateSelection(f => f.IsSelected = true);

        [RelayCommand]
        private void DeselectAll() => MutateSelection(f => f.IsSelected = false);

        [RelayCommand]
        private void InvertSelection() => MutateSelection(f => f.IsSelected = !f.IsSelected);

        // ----- notices and summary -----

        private void UpdateHeader()
        {
            int total = _files.Count;
            int displayed = Rows.Count;
            if (total == 0)
            {
                CountText = string.Empty;
                IgnoredText = string.Empty;
                return;
            }

            int ignoredCount = _files.Count(f => f.IsExcludedByRule);
            int selected = _files.Count(f => f.IsSelected && !f.IsExcludedByRule);

            CountText = displayed < total
                ? $"Showing {displayed:N0} of {total:N0}"
                : ignoredCount > 0 ? $"{selected:N0} items" : $"{total:N0} items";
            IgnoredText = ignoredCount == 0 ? string.Empty
                : displayed < total ? $"{ignoredCount:N0} ignored"
                : $"{ignoredCount:N0} folder{(ignoredCount == 1 ? "" : "s")} ignored";
        }

        private void UpdateSummary()
        {
            if (_files.Count == 0)
            {
                SummaryText = Service == null ? "Select a source folder to preview files." : "No files found to organize.";
                SummaryTone = SummaryTone.Muted;
                return;
            }

            var selectedFiles = _files.Where(f => f.IsSelected && !f.IsExcludedByRule).ToList();
            int selCount = selectedFiles.Count, excludedCount = _files.Count(f => f.IsExcludedByRule), unselCount = _files.Count(f => !f.IsSelected && !f.IsExcludedByRule);

            if (selCount == 0)
            {
                SummaryText = "No items selected to organize (all items are unchecked or excluded).";
                SummaryTone = SummaryTone.Warning;
                return;
            }

            var grouped = Service?.GroupByMonthYear(selectedFiles) ?? new Dictionary<string, List<FileItem>>();
            var fileCount = selectedFiles.Count(f => !f.IsDirectory);
            var folderCount = selectedFiles.Count(f => f.IsDirectory);

            string label = Settings.OrgMode == OrganizationMode.Category ? "category folder(s)" : (Settings.OrgMode == OrganizationMode.Extension ? "extension folder(s)" : (Settings.OrgMode == OrganizationMode.Date ? "date folder(s)" : "folder(s)"));
            string baseSummary = folderCount > 0
                ? $"Ready: {selCount:N0} selected ({fileCount:N0} files, {folderCount:N0} folders) → {grouped.Count:N0} target {label}"
                : $"Ready: {selCount:N0} selected file(s) → {grouped.Count:N0} target {label}";
            if (excludedCount > 0 || unselCount > 0) baseSummary += $" ({excludedCount + unselCount:N0} excluded/skipped)";
            SummaryText = baseSummary;
            SummaryTone = SummaryTone.Ready;
        }

        public void CheckConflicts()
        {
            if (Service == null || _files.Count == 0)
            {
                _conflicts = new();
                _conflictByItem = new();
                ConflictText = string.Empty;
                return;
            }

            var grouped = Service.GroupByMonthYear(_files);
            _conflicts = ConflictDetector.Detect(_files, grouped, OutputBase, Settings.CreateSortedSubfolder, Settings.Use24HourTimestamp);
            _conflictByItem = new Dictionary<FileItem, ConflictInfo>();
            foreach (var c in _conflicts) _conflictByItem.TryAdd(c.Item, c);
            ConflictText = _conflicts.Count > 0 ? $"{_conflicts.Count} collision/conflict(s) detected." : string.Empty;
        }

        private void CheckTimestampSimilarity()
        {
            if (Service == null || _files.Count < 3 || Settings.OrgMode == OrganizationMode.Category || Settings.OrgMode == OrganizationMode.Extension)
            {
                TimestampWarning = string.Empty;
                return;
            }

            var grouped = Service.GroupByMonthYear(_files);
            if (grouped.Count == 0) { TimestampWarning = string.Empty; return; }

            var dominant = grouped.OrderByDescending(g => g.Value.Count).First();
            double ratio = (double)dominant.Value.Count / _files.Count;
            bool isSimilar = ratio >= AppConstants.TimestampSimilarityThreshold;
            TimestampWarning = isSimilar ? $"{(int)(ratio * 100)}% of items share the same date/period and will be organized into '{dominant.Key}'." : string.Empty;
        }

        private void CheckExclusionsPausedWarning()
        {
            if (Settings.EnableFolderExclusions || !Settings.IncludeTopLevelFolders || _files.Count == 0)
            {
                ExclusionsPausedText = string.Empty;
                return;
            }

            int markerCount = IgnoreMarker.CountMarkedFolders(_files);
            ExclusionsPausedText = markerCount > 0
                ? $"Folder exclusions are paused. {markerCount:N0} ignored folder(s) will be organized."
                : string.Empty;
        }

        [RelayCommand]
        private async Task ReviewConflicts()
        {
            if (_conflicts.Count == 0) return;
            await DialogService.ShowAsync<object>(new ConflictDialog(_conflicts, reviewOnly: true));
        }

        [RelayCommand]
        private async Task ResumeExclusions()
        {
            Settings.EnableFolderExclusions = true;
            Settings.SaveToFile();
            await LoadAsync();
        }

        [RelayCommand]
        private void OpenExclusions() => WeakReferenceMessenger.Default.Send(new OpenSettingsMessage("Exclusions"));

        [RelayCommand]
        private void Browse() => WeakReferenceMessenger.Default.Send(new BrowseMessage());

        // ----- organization mode -----

        /// <summary>Targets again without a rescan (mode, naming or file type rules changed).</summary>
        public void ReapplyOrganizationMode()
        {
            ApplyNamingSettings();

            if (_files.Count > 0)
            {
                foreach (var file in _files)
                {
                    file.TargetFolder = TargetFolderResolver.Resolve(
                        file,
                        Settings.OrgMode,
                        Settings.FolderFormat,
                        Settings.FolderPrefix,
                        Settings.FolderSuffix,
                        Settings.CategoryPrefix,
                        Settings.CategorySuffix,
                        Settings.GroupGitRepositories);
                }
                if (Settings.OrgMode != OrganizationMode.Date)
                {
                    if (Settings.KeepHtmlCompanionsTogether)
                    {
                        TargetFolderResolver.ApplyHtmlCompanionPairing(_files);
                    }
                    if (Settings.KeepSubtitleCompanionsTogether)
                    {
                        TargetFolderResolver.ApplySubtitleCompanionPairing(_files, Settings.PackageVideoSubtitles);
                    }
                }
                CheckConflicts();
                BuildRows();
                UpdateSummary();
                CheckTimestampSimilarity();
            }
        }

        /// <summary>After the settings pages: a rescan when the scan itself changed, otherwise only new targets.</summary>
        public void ApplySettings(AppSettings before)
        {
            if (before.RequiresRescan(Settings) || before.EnableFolderExclusions != Settings.EnableFolderExclusions)
            {
                ApplyNamingSettings();
                _ = LoadAsync();
            }
            else
            {
                ReapplyOrganizationMode();
            }
        }

        // ----- context menu -----

        private bool HasSelection() => SelectedRow != null;

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task OpenSelected()
        {
            if (SelectedRow is not { } row) return;
            try
            {
                ShellActions.Open(row.Item.FullPath);
            }
            catch (Exception ex)
            {
                await DialogService.ErrorAsync("Error Opening Item", $"Could not open file:\n\n{ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task RevealSelected()
        {
            if (SelectedRow is not { } row) return;
            try
            {
                ShellActions.Reveal(row.Item.FullPath);
            }
            catch (Exception ex)
            {
                await DialogService.ErrorAsync("Error Opening Explorer", $"Could not open Explorer:\n\n{ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void CopySelectedPath()
        {
            if (SelectedRow is not { } row) return;
            try
            {
                Clipboard.SetText(row.Item.FullPath);
                _status.Show("Path copied to the clipboard");
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task RenameSelected()
        {
            if (SelectedRow is not { } row) return;
            var item = row.Item;
            string oldName = item.Name;
            string? parentDir = Path.GetDirectoryName(item.FullPath);
            if (string.IsNullOrEmpty(parentDir)) return;

            string? newName = await DialogService.PromptAsync("Rename Item", "Enter new name:", oldName, "Rename", selectNameOnly: true, validate: name =>
            {
                if (string.IsNullOrWhiteSpace(name)) return "Please enter a name.";
                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return "The name cannot contain any of the following characters:\n\\ / : * ? \" < > |";
                string target = Path.Combine(parentDir, name);
                if (!string.Equals(name, oldName, StringComparison.Ordinal) && (item.IsDirectory ? Directory.Exists(target) : File.Exists(target)))
                    return $"A {(item.IsDirectory ? "folder" : "file")} named '{name}' already exists in this location.";
                return null;
            });
            if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName, oldName, StringComparison.Ordinal)) return;

            try
            {
                string newPath = Path.Combine(parentDir, newName);
                if (item.IsDirectory) Directory.Move(item.FullPath, newPath);
                else File.Move(item.FullPath, newPath);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await DialogService.ErrorAsync("Rename Failed", $"Failed to rename item:\n\n{ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task DeleteSelected()
        {
            if (SelectedRow is not { } row) return;
            var item = row.Item;
            bool confirmed = await DialogService.ConfirmAsync(
                "Confirm Delete",
                $"Are you sure you want to send this {(item.IsDirectory ? "folder" : "file")} to the Recycle Bin?\n\n{item.Name}",
                "Move to Recycle Bin");
            if (!confirmed) return;

            try
            {
                ShellActions.SendToRecycleBin(item.FullPath);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await DialogService.ErrorAsync("Delete Failed", $"Failed to delete item:\n\n{ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ToggleIgnore()
        {
            if (SelectedRow is not { Item.IsDirectory: true } row || string.IsNullOrWhiteSpace(_state.SourceFolder)) return;
            string clean = row.Item.Name.Trim().TrimEnd('/', '\\');
            string folderPath = Path.Combine(_state.SourceFolder, clean);
            if (!Directory.Exists(folderPath)) return;

            try
            {
                if (IgnoreMarker.IsIgnored(folderPath))
                {
                    IgnoreMarker.Remove(folderPath);
                }
                else
                {
                    if (Settings.ShowIgnoreMarkerWarning)
                    {
                        var dialog = new AlertDialog(
                            DialogKind.Info,
                            $"Ignore Folder '{clean}'?",
                            $"TimeFold will place a small '{AppConstants.TimefoldIgnoreFileName}' marker file inside this folder so TimeFold can detect which folders to skip during organization.\n\n" +
                            $"Tip: You can also manually paste a '{AppConstants.TimefoldIgnoreFileName}' file into any folder at any time to exclude it from organization.",
                            "Ignore Folder",
                            "Cancel",
                            optionText: "Do not show this warning again");
                        if (!await DialogService.ShowAsync<bool>(dialog)) return;
                        if (dialog.IsOptionChecked) Settings.ShowIgnoreMarkerWarning = false;
                    }
                    IgnoreMarker.Add(folderPath);
                    Settings.EnableFolderExclusions = true;
                    Settings.SaveToFile();
                }
            }
            catch (Exception ex)
            {
                await DialogService.WarningAsync("Ignore Folder", $"Failed to update folder ignore marker: {ex.Message}");
            }

            await LoadAsync();
        }

        [RelayCommand]
        private async Task RenameCategory()
        {
            if (SelectedRow is not { } row || row.Item.IsDirectory || string.IsNullOrWhiteSpace(row.Item.Extension)) return;
            string currentCategory = FileTypeService.Instance.GetCategory(row.Item.Extension.ToLowerInvariant());

            string? newName = await DialogService.PromptAsync("Rename Category", $"Enter new name for category '{currentCategory}':", currentCategory);
            if (!string.IsNullOrWhiteSpace(newName) && !string.Equals(newName, currentCategory, StringComparison.OrdinalIgnoreCase))
            {
                FileTypeService.Instance.RenameCategory(currentCategory, newName);
                ReapplyOrganizationMode();
            }
        }

        [RelayCommand]
        private void ResetCategoryName()
        {
            if (SelectedRow is not { } row || row.Item.IsDirectory || string.IsNullOrWhiteSpace(row.Item.Extension)) return;
            if (FileTypeService.Instance.ResetCategoryName(FileTypeService.Instance.GetCategory(row.Item.Extension.ToLowerInvariant())))
                ReapplyOrganizationMode();
        }

        [RelayCommand]
        private async Task ChangeCategory()
        {
            if (SelectedRow is not { } row) return;
            var item = row.Item;
            var types = FileTypeService.Instance;

            if (item.IsDirectory)
            {
                bool isGit = item.IsGitRepository && Settings.GroupGitRepositories;
                string label = isGit ? "Git repositories" : "folders";
                bool hasCustom = types.IsFolderCategoryOverridden(isGit, out var curCat);
                var choice = await CategoryPickerDialog.ShowAsync(label, curCat, types.GetAllCategories(), hasCustom);
                if (choice.Reset) { types.ResetFolderCategoryOverride(isGit); ReapplyOrganizationMode(); }
                else if (!string.IsNullOrWhiteSpace(choice.Category)) { types.SetFolderCategoryOverride(isGit, choice.Category); ReapplyOrganizationMode(); }
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Extension)) return;
            string ext = item.Extension.ToLowerInvariant();
            var picked = await CategoryPickerDialog.ShowAsync(ext, types.GetCategory(ext), types.GetAllCategories(), types.IsCustomRoute(ext));
            if (picked.Reset) { types.RemoveCategoryOverride(ext); ReapplyOrganizationMode(); }
            else if (!string.IsNullOrWhiteSpace(picked.Category)) { types.SetCategoryOverride(ext, picked.Category); ReapplyOrganizationMode(); }
        }
    }
}
