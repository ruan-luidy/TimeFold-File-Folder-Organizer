using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TimeFold.Core.Config;
using TimeFold.Core.Settings;
using TimeFold.Wpf.Features.Modes;
using TimeFold.Wpf.Features.Organize;
using TimeFold.Wpf.Features.Preview;
using TimeFold.Wpf.Features.Settings;
using TimeFold.Wpf.Features.Source;
using TimeFold.Wpf.Features.Tour;
using TimeFold.Wpf.Features.Undo;
using TimeFold.Wpf.Features.Updates;
using TimeFold.Wpf.Shared.Progress;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Shell
{
    /// <summary>
    /// The window: the organizer page and the settings page, the top bar, the status bar and startup
    /// (what MainForm.cs did besides the list).
    /// </summary>
    public sealed partial class MainViewModel : ObservableObject, IRecipient<OpenSettingsMessage>, IRecipient<BrowseMessage>
    {
        private readonly AppState _state;
        private readonly UpdateChecker _updates;
        private AppSettings? _settingsBefore;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOrganizePage))]
        private bool _isSettingsPage;

        public MainViewModel(
            AppState state,
            StatusService status,
            SourceViewModel source,
            ModesViewModel modes,
            PreviewViewModel preview,
            OrganizeViewModel organize,
            UndoViewModel undo,
            ProgressViewModel progress,
            SettingsViewModel settings,
            UpdateChecker updates)
        {
            _state = state;
            _updates = updates;
            Status = status;
            Source = source;
            Modes = modes;
            Preview = preview;
            Organize = organize;
            Undo = undo;
            Progress = progress;
            Settings = settings;

            WeakReferenceMessenger.Default.RegisterAll(this);
            _state.SettingsChanged += (_, _) => OnPropertyChanged(nameof(Topmost));
            Settings.TourRequested += async (_, _) => await ShowTourAsync(firstRun: false);
            Status.PropertyChanged += OnStatusChanged;
            Preview.PropertyChanged += OnStatusChanged;
        }

        public StatusService Status { get; }

        public SourceViewModel Source { get; }

        public ModesViewModel Modes { get; }

        public PreviewViewModel Preview { get; }

        public OrganizeViewModel Organize { get; }

        public UndoViewModel Undo { get; }

        public ProgressViewModel Progress { get; }

        public SettingsViewModel Settings { get; }

        public bool IsOrganizePage => !IsSettingsPage;

        public bool Topmost => _state.Settings.ShowOnTop;

        public string VersionText => $"TimeFold {AppConstants.AppVersion}";

        /// <summary>The last notice, or the plan summary when there is none.</summary>
        public string StatusText => Status.Message.Length > 0 ? Status.Message : Preview.SummaryText;

        private void OnStatusChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(StatusService.Message) or nameof(PreviewViewModel.SummaryText))
                OnPropertyChanged(nameof(StatusText));
        }

        public void Receive(OpenSettingsMessage message) => OpenSettings(message.Page);

        public void Receive(BrowseMessage message) => Browse();

        [RelayCommand]
        private void OpenSettings(string? page)
        {
            if (Progress.IsActive) return;
            if (!IsSettingsPage) _settingsBefore = _state.Snapshot();
            Settings.Open(page);
            IsSettingsPage = true;
        }

        /// <summary>Back to the organizer: the plan picks up whatever changed in the settings.</summary>
        [RelayCommand]
        private void ShowOrganizer()
        {
            if (!IsSettingsPage) return;
            IsSettingsPage = false;
            Source.Refresh();
            Modes.Refresh();
            Organize.RefreshLabels();
            Undo.RefreshState();
            if (_settingsBefore != null && Preview.HasSource) Preview.ApplySettings(_settingsBefore);
            _settingsBefore = null;
        }

        [RelayCommand]
        private void ToggleSettings()
        {
            if (IsSettingsPage) ShowOrganizer();
            else OpenSettings(null);
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            _state.Settings.DarkMode = !_state.Settings.DarkMode;
            _state.Save();
            ThemeService.Apply(_state.Settings.DarkMode);
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (!Progress.IsActive && IsOrganizePage) await Preview.LoadAsync();
        }

        [RelayCommand]
        private void NewProject()
        {
            if (Progress.IsActive) return;
            ShowOrganizer();
            Organize.StartNew();
        }

        [RelayCommand]
        private void Browse()
        {
            if (Progress.IsActive) return;
            ShowOrganizer();
            Source.BrowseCommand.Execute(null);
        }

        [RelayCommand]
        private Task ShowTour() => ShowTourAsync(firstRun: false);

        [RelayCommand]
        private Task CheckForUpdates() => _updates.CheckManuallyAsync();

        [RelayCommand]
        private void ShowAbout() => OpenSettings("About");

        /// <summary>
        /// After the window is up: the folder from the Explorer context menu (or the app folder when
        /// auto-load is on), then the tour on the first run or the update check.
        /// </summary>
        public async Task StartAsync(string? initialFolder)
        {
            if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
                Source.SetSourceFolder(initialFolder);
            else if (_state.Settings.AutoLoadExeDirectoryOnStartup && Directory.Exists(SourceViewModel.ExecutableDirectory))
                Source.SetSourceFolder(SourceViewModel.ExecutableDirectory);

            if (!_state.Settings.HasSeenWelcomeTour)
            {
                await ShowTourAsync(firstRun: true);
                if (_state.Settings.CheckForUpdatesOnStartup)
                    _ = _updates.CheckOnStartupAsync(1500);
            }
            else if (_state.Settings.CheckForUpdatesOnStartup)
            {
                _ = _updates.CheckOnStartupAsync(2500);
            }
        }

        private async Task ShowTourAsync(bool firstRun)
        {
            var settings = _state.Settings;
            var dialog = new TourDialog(settings.DarkMode, settings.HasSeenWelcomeTour, ThemeService.Apply);
            var result = await DialogService.ShowAsync<TourResult?>(dialog);

            // closing the tour keeps the theme picked in it
            settings.DarkMode = result?.DarkMode ?? ThemeService.IsDark;
            settings.HasSeenWelcomeTour = firstRun || (result?.DontShowOnStartup ?? settings.HasSeenWelcomeTour);
            _state.Save();
            ThemeService.Apply(settings.DarkMode);
            if (IsSettingsPage) Settings.Open(null);
        }
    }
}
