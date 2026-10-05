using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TimeFold.Core.Config;
using TimeFold.Wpf.Features.Preview;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Source
{
    public sealed record RecentFolder(string Name, string Path);

    /// <summary>
    /// Where the files come from and where the sorted folders go: browse, recent, drop, the app folder,
    /// the destination and the Sorted_ subfolder (the top of MainForm).
    /// </summary>
    public sealed partial class SourceViewModel : ObservableObject
    {
        private readonly AppState _state;
        private readonly PreviewViewModel _preview;
        private readonly StatusService _status;

        [ObservableProperty]
        private bool _isDragOver;

        public SourceViewModel(AppState state, PreviewViewModel preview, StatusService status)
        {
            _state = state;
            _preview = preview;
            _status = status;
            _state.PropertyChanged += OnStateChanged;
            UpdateRecent();
        }

        public static string ExecutableDirectory =>
            Path.GetDirectoryName(Environment.ProcessPath) ?? Environment.CurrentDirectory;

        public string SourceFolder => _state.SourceFolder;

        public bool HasSource => !string.IsNullOrEmpty(_state.SourceFolder);

        public string SourceName => HasSource ? (Path.GetFileName(SourceFolder.TrimEnd('\\')) is { Length: > 0 } name ? name : SourceFolder) : string.Empty;

        public ObservableCollection<RecentFolder> RecentFolders { get; } = new();

        public bool HasRecent => RecentFolders.Count > 0;

        public bool UseSourceAsOutput
        {
            get => _state.Settings.UseSourceAsOutput;
            set
            {
                if (_state.Settings.UseSourceAsOutput == value) return;
                _state.Settings.UseSourceAsOutput = value;
                OnPropertyChanged();
                _state.Save();
                OnOutputChanged();
            }
        }

        public bool CreateSortedSubfolder
        {
            get => _state.Settings.CreateSortedSubfolder;
            set
            {
                if (_state.Settings.CreateSortedSubfolder == value) return;
                _state.Settings.CreateSortedSubfolder = value;
                if (_preview.Service != null) _preview.Service.CreateSortedSubfolder = value;
                OnPropertyChanged();
                _state.Save();
                OnOutputChanged();
            }
        }

        public bool IncludeFolders
        {
            get => _state.Settings.IncludeTopLevelFolders;
            set
            {
                if (_state.Settings.IncludeTopLevelFolders == value) return;
                _state.Settings.IncludeTopLevelFolders = value;
                OnPropertyChanged();
                _state.Save();
                _ = _preview.LoadAsync();
            }
        }

        /// <summary>The folder that will be created (or used directly), as in the old output box.</summary>
        public string OutputText
        {
            get
            {
                string baseDir = _state.OutputBase;
                if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) return string.Empty;
                return _state.Settings.CreateSortedSubfolder
                    ? AppConstants.GetSortedFolderPreviewPath(baseDir, _state.Settings.Use24HourTimestamp)
                    : baseDir;
            }
        }

        public string OutputTooltip => _state.Settings.CreateSortedSubfolder
            ? $"Output Destination (will create):\n{OutputText}"
            : $"Output Destination (direct into folder):\n{OutputText}";

        private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SourceFolder))
            {
                OnPropertyChanged(nameof(SourceFolder));
                OnPropertyChanged(nameof(HasSource));
                OnPropertyChanged(nameof(SourceName));
            }
            if (e.PropertyName is nameof(AppState.SourceFolder) or nameof(AppState.OutputBase))
                OnOutputChanged();
        }

        private void OnOutputChanged()
        {
            OnPropertyChanged(nameof(OutputText));
            OnPropertyChanged(nameof(OutputTooltip));
        }

        /// <summary>Settings changed elsewhere (settings page, tour): read them again.</summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(UseSourceAsOutput));
            OnPropertyChanged(nameof(CreateSortedSubfolder));
            OnPropertyChanged(nameof(IncludeFolders));
            OnOutputChanged();
            UpdateRecent();
        }

        public void SetSourceFolder(string folder)
        {
            if (!Directory.Exists(folder)) return;
            _state.Settings.AddRecentFolder(folder);
            UpdateRecent();

            // the same folder again (after an undo, a rename) still rescans
            if (string.Equals(_state.SourceFolder, folder, StringComparison.OrdinalIgnoreCase))
                _state.SourceFolder = string.Empty;
            _state.SourceFolder = folder;
        }

        [RelayCommand]
        private void Browse()
        {
            var dialog = new OpenFolderDialog { Title = "Select folder to organize" };
            if (dialog.ShowDialog() == true) SetSourceFolder(dialog.FolderName);
        }

        [RelayCommand]
        private void UseCurrentFolder() => SetSourceFolder(ExecutableDirectory);

        [RelayCommand]
        private async Task OpenRecent(RecentFolder? recent)
        {
            if (recent == null) return;
            if (Directory.Exists(recent.Path))
            {
                SetSourceFolder(recent.Path);
                return;
            }

            await DialogService.WarningAsync("Folder Not Found", $"The folder could not be found:\n\n{recent.Path}\n\nIt may have been moved, deleted, or on an unplugged drive.");
            _state.Settings.RemoveRecentFolder(recent.Path);
            UpdateRecent();
        }

        [RelayCommand]
        private void ClearRecent()
        {
            _state.Settings.ClearRecentFolders();
            UpdateRecent();
            _status.Show("Recent history cleared");
        }

        [RelayCommand]
        private void BrowseOutput()
        {
            var dialog = new OpenFolderDialog { Title = "Select output folder (where Sorted folder will be created)" };
            if (dialog.ShowDialog() == true) _state.CustomOutputFolder = dialog.FolderName;
        }

        [RelayCommand]
        private void OpenSourceInExplorer()
        {
            if (HasSource) ShellActions.OpenFolder(SourceFolder);
        }

        /// <summary>A dropped folder, or the folder of a dropped file.</summary>
        public void Drop(string path)
        {
            if (Directory.Exists(path)) SetSourceFolder(path);
            else if (File.Exists(path) && Path.GetDirectoryName(path) is { Length: > 0 } dir && Directory.Exists(dir)) SetSourceFolder(dir);
        }

        /// <summary>File > New: back to an empty form with the default destination options.</summary>
        public void Reset()
        {
            _state.CustomOutputFolder = string.Empty;
            _state.SourceFolder = string.Empty;
            _state.Settings.UseSourceAsOutput = AppConstants.DefaultUseSourceAsOutput;
            _state.Settings.CreateSortedSubfolder = AppConstants.DefaultCreateSortedSubfolder;
            _state.Settings.IncludeTopLevelFolders = AppConstants.DefaultIncludeTopLevelFolders;
            _state.Save();
            Refresh();
        }

        private void UpdateRecent()
        {
            RecentFolders.Clear();
            foreach (var path in _state.Settings.RecentFolders)
            {
                string name = Path.GetFileName(path);
                RecentFolders.Add(new RecentFolder(string.IsNullOrEmpty(name) ? path : name, path));
            }
            OnPropertyChanged(nameof(HasRecent));
        }
    }
}
