using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TimeFold.Core.Config;
using TimeFold.Core.Settings;
using TimeFold.Wpf.Features.Preview;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Modes
{
    /// <summary>
    /// How the files are arranged: the five modes, the date format badge and the folder rules
    /// (MainForm.OrgModes: the mode selector and the folder rules menu).
    /// </summary>
    public sealed partial class ModesViewModel : ObservableObject
    {
        private readonly AppState _state;
        private readonly PreviewViewModel _preview;

        public ModesViewModel(AppState state, PreviewViewModel preview)
        {
            _state = state;
            _preview = preview;
        }

        private AppSettings Settings => _state.Settings;

        public OrganizationMode Mode
        {
            get => Settings.OrgMode;
            set
            {
                if (Settings.OrgMode == value) return;
                Settings.OrgMode = value;
                _state.Save();
                Refresh();
                _preview.ReapplyOrganizationMode();
            }
        }

        public bool IsDateMode => Settings.OrgMode == OrganizationMode.Date;

        public bool UsesDate => Settings.OrgMode != OrganizationMode.Category && Settings.OrgMode != OrganizationMode.Extension;

        private string SampleDate => AppConstants.FormatFolderDate(DateTime.Now, Settings.FolderFormat, Settings.FolderPrefix, Settings.FolderSuffix);

        public string DateExample => SampleDate;

        public string CategoryAndDateExample => Path.Combine("Images", SampleDate);

        public string DateAndCategoryExample => Path.Combine(SampleDate, "Images");

        public string FormatBadge
        {
            get
            {
                if (!UsesDate) return "Format: N/A";
                string sample = SampleDate;
                return sample.Length > 35 ? sample[..33].TrimEnd() + "…" : sample;
            }
        }

        public string FormatTooltip => UsesDate
            ? $"Active Format: {SampleDate}\nClick to customize the naming template"
            : "Date format is not used in this organization mode.";

        public bool GroupGitRepositories
        {
            get => Settings.GroupGitRepositories;
            set => SetRule(() => Settings.GroupGitRepositories = value);
        }

        public bool KeepHtmlCompanionsTogether
        {
            get => Settings.KeepHtmlCompanionsTogether;
            set => SetRule(() => Settings.KeepHtmlCompanionsTogether = value);
        }

        public bool PackageVideoSubtitles
        {
            get => Settings.PackageVideoSubtitles;
            set => SetRule(() => Settings.PackageVideoSubtitles = value);
        }

        public string ExclusionsText
        {
            get
            {
                int ruleCount = Settings.ExcludedFolderNames?.Count ?? 0;
                string status = !Settings.EnableFolderExclusions ? " (Disabled)" : (ruleCount > 0 ? $" ({ruleCount} defined)" : "");
                return $"Folder exclusion rules{status}";
            }
        }

        public string RulesSummary
        {
            get
            {
                if (IsDateMode) return "Date mode: timeline routing active. Folder grouping rules apply in Smart Category & Extension modes.";
                var active = new List<string>();
                if (Settings.GroupGitRepositories) active.Add("Git repositories grouped into 'Git Repos'");
                if (Settings.KeepHtmlCompanionsTogether) active.Add("HTML companion folders kept with HTML files");
                if (Settings.PackageVideoSubtitles) active.Add("Matching video & subtitle pairs packaged");
                if (Settings.OrgMode == OrganizationMode.Extension) active.Add("Other loose folders placed into 'Folders'");
                return active.Count == 0 ? "No folder rules active. Default folder routing applied." : string.Join("\n", active.Select(a => "• " + a));
            }
        }

        private void SetRule(Action apply)
        {
            apply();
            _state.Save();
            Refresh();
            _preview.ReapplyOrganizationMode();
        }

        [RelayCommand]
        private void SwitchToCategory() => Mode = OrganizationMode.Category;

        [RelayCommand]
        private void CustomizeNaming()
        {
            if (UsesDate) WeakReferenceMessenger.Default.Send(new OpenSettingsMessage("Naming"));
        }

        [RelayCommand]
        private void CustomizeFileTypes() => WeakReferenceMessenger.Default.Send(new OpenSettingsMessage("FileTypes"));

        [RelayCommand]
        private void OpenExclusions() => WeakReferenceMessenger.Default.Send(new OpenSettingsMessage("Exclusions"));

        /// <summary>Everything here is read from the settings, so one notification redraws it all.</summary>
        public void Refresh() => OnPropertyChanged(string.Empty);
    }
}
