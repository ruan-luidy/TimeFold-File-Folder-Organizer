using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeFold.Core.Exclusions;
using TimeFold.Wpf.Features.Preview;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Exclusions
{
    /// <summary>
    /// Folder exclusion rules (FolderExclusionDialog). The list is the subfolders of the source folder that carry a
    /// .timefold-ignore marker; Save &amp; Apply writes and removes the markers, so it stays an explicit step.
    /// </summary>
    public sealed partial class ExclusionsViewModel : ObservableObject
    {
        private readonly AppState _state;
        private readonly PreviewViewModel _preview;
        private readonly StatusService _status;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasChanges))]
        private bool _enableExclusions;

        [ObservableProperty]
        private string _newFolder = string.Empty;

        [ObservableProperty]
        private string? _selectedFolder;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasChanges))]
        private bool _listChanged;

        public ExclusionsViewModel(AppState state, PreviewViewModel preview, StatusService status)
        {
            _state = state;
            _preview = preview;
            _status = status;
            Folders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
            Load();
        }

        public ObservableCollection<string> Folders { get; } = new();

        public bool HasSource => !string.IsNullOrWhiteSpace(_state.SourceFolder);

        public string SourceFolder => _state.SourceFolder;

        public bool IsEmpty => Folders.Count == 0;

        public bool HasChanges => ListChanged || EnableExclusions != _state.Settings.EnableFolderExclusions;

        public void Load()
        {
            EnableExclusions = _state.Settings.EnableFolderExclusions;
            Folders.Clear();
            foreach (var name in IgnoreMarker.ListIgnored(_state.SourceFolder)) Folders.Add(name);
            ListChanged = false;
            OnPropertyChanged(nameof(HasSource));
            OnPropertyChanged(nameof(SourceFolder));
        }

        [RelayCommand]
        private async Task Add()
        {
            string name = NewFolder.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            if (Folders.Any(f => string.Equals(f, name, StringComparison.OrdinalIgnoreCase)))
            {
                await DialogService.InfoAsync("Duplicate Name", $"'{name}' is already in the exclusion list.");
                return;
            }

            Folders.Add(name);
            NewFolder = string.Empty;
            ListChanged = true;
        }

        [RelayCommand]
        private void Remove(string? name)
        {
            if (name != null && Folders.Remove(name)) ListChanged = true;
        }

        [RelayCommand]
        private async Task Clear()
        {
            if (Folders.Count == 0) return;
            if (await DialogService.ConfirmAsync("Clear Exclusions", "Clear all excluded folder rules?", "Clear All"))
            {
                Folders.Clear();
                ListChanged = true;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            _state.Settings.ExcludedFolderNames = new();
            _state.Settings.EnableFolderExclusions = EnableExclusions;
            _state.Save();
            IgnoreMarker.Apply(_state.SourceFolder, Folders);
            Load();
            _status.Show("Exclusion rules saved");
            await _preview.LoadAsync();
        }
    }
}
