using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeFold.Core.Settings;

namespace TimeFold.Wpf.Shared.Services
{
    /// <summary>
    /// What the screens share: the settings (same settings.json as the WinForms build), the folder being organized
    /// and where the sorted folders go.
    /// </summary>
    public sealed partial class AppState : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OutputBase))]
        private string _sourceFolder = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OutputBase))]
        private string _customOutputFolder = string.Empty;

        public AppSettings Settings { get; private set; } = AppSettings.LoadFromFile();

        /// <summary>The folder the Sorted_ folder (or the groups) is created in.</summary>
        public string OutputBase => Settings.UseSourceAsOutput ? SourceFolder.Trim() : CustomOutputFolder.Trim();

        /// <summary>Raised after a setting changed and was saved.</summary>
        public event EventHandler? SettingsChanged;

        public void Save()
        {
            Settings.SaveToFile();
            OnPropertyChanged(nameof(OutputBase));
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Reload()
        {
            Settings = AppSettings.LoadFromFile();
            OnPropertyChanged(nameof(OutputBase));
        }

        // A copy to compare against later (RequiresRescan); the settings object itself is edited in place
        public AppSettings Snapshot() =>
            JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(Settings)) ?? new AppSettings();
    }
}
