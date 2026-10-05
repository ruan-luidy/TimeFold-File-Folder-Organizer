using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeFold.Core.Config;
using TimeFold.Core.Settings;
using TimeFold.Wpf.Features.Exclusions;
using TimeFold.Wpf.Features.Explorer;
using TimeFold.Wpf.Features.FileTypes;
using TimeFold.Wpf.Features.Naming;
using TimeFold.Wpf.Features.Updates;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Settings
{
    public sealed record ChoiceOption<T>(T Value, string Label);

    /// <summary>
    /// The settings pages (the old Preferences, Type Rules, Smart Pairing, Exclusions and About windows).
    /// Changes are saved as they are made; the plan is refreshed when the user goes back to it.
    /// </summary>
    public sealed partial class SettingsViewModel : ObservableObject
    {
        private readonly AppState _state;
        private readonly UpdateChecker _updates;

        [ObservableProperty]
        private string _page = "Dates";

        public SettingsViewModel(AppState state, UpdateChecker updates, NamingViewModel naming, FileTypesViewModel fileTypes, ExclusionsViewModel exclusions)
        {
            _state = state;
            _updates = updates;
            Naming = naming;
            FileTypes = fileTypes;
            Exclusions = exclusions;
        }

        public NamingViewModel Naming { get; }

        public FileTypesViewModel FileTypes { get; }

        public ExclusionsViewModel Exclusions { get; }

        private AppSettings Settings => _state.Settings;

        /// <summary>Asks the shell to replay the welcome tour.</summary>
        public event EventHandler? TourRequested;

        /// <summary>Opened from the top bar or a link: everything is read again from the settings.</summary>
        public void Open(string? page)
        {
            if (page != null) Page = page;
            Naming.Load();
            FileTypes.Load();
            Exclusions.Load();
            OnPropertyChanged(string.Empty);
        }

        private void Set(Action apply, [CallerMemberName] string? property = null)
        {
            apply();
            _state.Save();
            OnPropertyChanged(property);
        }

        // ----- dates and folders -----

        public IReadOnlyList<ChoiceOption<DateSource>> DateSources { get; } =
        [
            new(DateSource.Modified, "Date Modified (Default)"),
            new(DateSource.Created, "Date Created"),
            new(DateSource.Earliest, "Earliest Date (Oldest)"),
        ];

        public ChoiceOption<DateSource> FileDateSource
        {
            get => DateSources.First(o => o.Value == Settings.FileDateSource);
            set => Set(() => Settings.FileDateSource = value.Value);
        }

        public ChoiceOption<DateSource> FolderDateSource
        {
            get => DateSources.First(o => o.Value == Settings.FolderDateSource);
            set => Set(() => Settings.FolderDateSource = value.Value);
        }

        public string MediaDateLabel => AppConstants.MediaDateTakenSettingLabel;

        public string MediaDateTooltip => AppConstants.MediaDateTakenSettingTooltip;

        public bool UseMediaDateTaken
        {
            get => Settings.UseMediaDateTaken;
            set => Set(() => Settings.UseMediaDateTaken = value);
        }

        public bool IncludeTopLevelFolders
        {
            get => Settings.IncludeTopLevelFolders;
            set => Set(() => Settings.IncludeTopLevelFolders = value);
        }

        public bool GroupGitRepositories
        {
            get => Settings.GroupGitRepositories;
            set => Set(() => Settings.GroupGitRepositories = value);
        }

        public bool KeepHtmlCompanionsTogether
        {
            get => Settings.KeepHtmlCompanionsTogether;
            set => Set(() => Settings.KeepHtmlCompanionsTogether = value);
        }

        public bool PackageVideoSubtitles
        {
            get => Settings.PackageVideoSubtitles;
            set => Set(() => Settings.PackageVideoSubtitles = value);
        }

        public bool IgnoreSystemFiles
        {
            get => Settings.IgnoreSystemFiles;
            set => Set(() => Settings.IgnoreSystemFiles = value);
        }

        // ----- general -----

        public bool ShowDetailedProgress
        {
            get => Settings.ShowDetailedProgress;
            set => Set(() => Settings.ShowDetailedProgress = value);
        }

        public bool GenerateCsvLog
        {
            get => Settings.GenerateCsvLog;
            set => Set(() => Settings.GenerateCsvLog = value);
        }

        public bool ShowOnTop
        {
            get => Settings.ShowOnTop;
            set => Set(() => Settings.ShowOnTop = value);
        }

        public bool Use24HourTimestamp
        {
            get => Settings.Use24HourTimestamp;
            set => Set(() => Settings.Use24HourTimestamp = value);
        }

        public bool AutoLoadExeDirectoryOnStartup
        {
            get => Settings.AutoLoadExeDirectoryOnStartup;
            set => Set(() => Settings.AutoLoadExeDirectoryOnStartup = value);
        }

        public bool CheckForUpdatesOnStartup
        {
            get => Settings.CheckForUpdatesOnStartup;
            set => Set(() => Settings.CheckForUpdatesOnStartup = value);
        }

        public IReadOnlyList<ChoiceOption<int>> PreviewLimits { get; } =
        [
            new(1000, "1,000 items (Default)"),
            new(2000, "2,000 items"),
            new(5000, "5,000 items"),
            new(10000, "10,000 items"),
            new(20000, "20,000 items"),
            new(50000, "50,000 items"),
            new(100000, "100,000 items (Max)"),
        ];

        public ChoiceOption<int> PreviewLimit
        {
            get => PreviewLimits.FirstOrDefault(o => o.Value == Settings.MaxPreviewItems) ?? PreviewLimits[0];
            set
            {
                Set(() => Settings.MaxPreviewItems = value.Value);
                OnPropertyChanged(nameof(ShowPreviewLimitWarning));
            }
        }

        public bool ShowPreviewLimitWarning => Settings.MaxPreviewItems > 2000;

        public string SortedFolderExample => AppConstants.GetSortedFolderPreviewPattern(Settings.Use24HourTimestamp);

        // ----- appearance -----

        public bool IsDark
        {
            get => Settings.DarkMode;
            set
            {
                if (Settings.DarkMode == value) return;
                Set(() => Settings.DarkMode = value);
                OnPropertyChanged(nameof(IsLight));
                ThemeService.Apply(value);
            }
        }

        public bool IsLight
        {
            get => !Settings.DarkMode;
            set
            {
                if (value) IsDark = false;
            }
        }

        // ----- explorer -----

        public bool IsInstalledBuild => ExplorerIntegration.IsInstalledBuild;

        public bool IsContextMenuActive => ExplorerIntegration.IsContextMenuRegistered;

        public string ConfigFolder => AppConstants.GetConfigDirectoryPath();

        [RelayCommand]
        private void OpenConfigLocation() => ExplorerIntegration.OpenConfigLocation();

        [RelayCommand]
        private void OpenReleases() => ShellActions.OpenUrl(AppConstants.ReleasesPageUrl);

        // ----- about -----

        public string AppName => AppConstants.AppName;

        public string VersionText => $"Version {AppConstants.AppVersion} (Build {AppConstants.BuildNumber})";

        public string Description => AppConstants.AppDescription;

        public string Author => AppConstants.Author;

        public string RepositoryUrl => AppConstants.RepositoryUrl;

        public string LicenseText => AppConstants.LicenseText;

        [RelayCommand]
        private void OpenRepository() => ShellActions.OpenUrl(AppConstants.RepositoryUrl);

        [RelayCommand]
        private Task CheckForUpdates() => _updates.CheckManuallyAsync();

        [RelayCommand]
        private void ShowTour() => TourRequested?.Invoke(this, EventArgs.Empty);

        // ----- defaults -----

        /// <summary>The Defaults button of the old Preferences (now applied at once, so it asks first).</summary>
        [RelayCommand]
        private async Task RestoreDefaults()
        {
            bool confirmed = await DialogService.ConfirmAsync(
                "Restore Defaults",
                "Put every preference back to its default (organization mode, naming, date sources, behavior and theme)? File type rules and exclusions are not touched.",
                "Restore Defaults");
            if (!confirmed) return;

            Settings.IncludeTopLevelFolders = AppConstants.DefaultIncludeTopLevelFolders;
            Settings.GroupGitRepositories = AppConstants.DefaultGroupGitRepositories;
            Settings.CreateSortedSubfolder = AppConstants.DefaultCreateSortedSubfolder;
            Settings.UseSourceAsOutput = AppConstants.DefaultUseSourceAsOutput;
            Settings.IgnoreSystemFiles = AppConstants.DefaultIgnoreSystemFiles;
            Settings.PackageVideoSubtitles = AppConstants.DefaultPackageVideoSubtitles;
            Settings.KeepSubtitleCompanionsTogether = AppConstants.DefaultKeepSubtitleCompanionsTogether;
            Settings.KeepHtmlCompanionsTogether = AppConstants.DefaultKeepHtmlCompanionsTogether;
            Settings.ShowDetailedProgress = AppConstants.DefaultShowDetailedProgress;
            Settings.ShowOnTop = AppConstants.DefaultShowOnTop;
            Settings.GenerateCsvLog = AppConstants.DefaultGenerateCsvLog;
            Settings.Use24HourTimestamp = AppConstants.DefaultUse24HourTimestamp;
            Settings.AutoLoadExeDirectoryOnStartup = AppConstants.DefaultAutoLoadExeDirectoryOnStartup;
            Settings.CheckForUpdatesOnStartup = true;
            Settings.DarkMode = AppConstants.DefaultDarkMode;
            Settings.FileDateSource = AppConstants.DefaultFileDateSource;
            Settings.FolderDateSource = AppConstants.DefaultFolderDateSource;
            Settings.UseMediaDateTaken = AppConstants.DefaultUseMediaDateTaken;
            Settings.FolderPrefix = AppConstants.DefaultFolderPrefix;
            Settings.FolderSuffix = AppConstants.DefaultFolderSuffix;
            Settings.MaxPreviewItems = AppConstants.DefaultMaxPreviewItems;
            Settings.FolderFormat = AppConstants.DefaultFolderFormat;
            Settings.OrgMode = OrganizationMode.Date;
            Settings.CategoryPrefix = AppConstants.DefaultCategoryPrefix;
            Settings.CategorySuffix = AppConstants.DefaultCategorySuffix;
            Settings.HasSeenWelcomeTour = true;
            _state.Save();

            ThemeService.Apply(Settings.DarkMode);
            Open(null);
        }
    }
}
