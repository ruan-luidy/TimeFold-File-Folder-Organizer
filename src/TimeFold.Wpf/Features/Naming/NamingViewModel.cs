using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeFold.Core.Config;
using TimeFold.Core.Naming;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Naming
{
    public sealed record FormatOptionItem(string Group, CoreFormat Core, string DisplayName);

    /// <summary>The date naming template of the old Preferences: layout, order, short months, prefix and suffix.</summary>
    public sealed partial class NamingViewModel : ObservableObject
    {
        private readonly AppState _state;
        private bool _loading;

        [ObservableProperty]
        private IReadOnlyList<FormatOptionItem> _options = [];

        [ObservableProperty]
        private FormatOptionItem? _selectedOption;

        [ObservableProperty]
        private bool _isFlipped;

        [ObservableProperty]
        private bool _useShortMonth;

        [ObservableProperty]
        private string _prefix = string.Empty;

        [ObservableProperty]
        private string _suffix = string.Empty;

        [ObservableProperty]
        private IReadOnlyList<string> _samples = [];

        public NamingViewModel(AppState state)
        {
            _state = state;
            Load();
        }

        public bool CanFlip => SelectedOption != null && DateFormatChoice.CanFlip(SelectedOption.Core);

        public bool CanShortMonth => SelectedOption != null && DateFormatChoice.CanShortMonth(SelectedOption.Core);

        public string FlipText => CanFlip ? (IsFlipped ? "Order: Flipped" : "Order: Standard") : "Flip (N/A)";

        public int MaxLength => AppConstants.MaxPrefixSuffixLength;

        /// <summary>Reads the saved format back into the controls (page opened, defaults restored).</summary>
        public void Load()
        {
            _loading = true;
            var (core, flipped, shortMonth) = DateFormatChoice.Split(_state.Settings.FolderFormat);
            IsFlipped = flipped;
            UseShortMonth = shortMonth;
            Prefix = _state.Settings.FolderPrefix;
            Suffix = _state.Settings.FolderSuffix;
            RebuildOptions(core);
            _loading = false;
            UpdatePreview();
        }

        private void RebuildOptions(CoreFormat selected)
        {
            int year = DateTime.Now.Year;
            Options = DateFormatChoice.Options
                .Select(o => new FormatOptionItem(o.Group, o.Core, DateFormatChoice.GetDisplayName(o.Core, IsFlipped, UseShortMonth, year)))
                .ToList();
            SelectedOption = Options.FirstOrDefault(o => o.Core == selected) ?? Options.First(o => o.Core == CoreFormat.YearMonth);
        }

        partial void OnSelectedOptionChanged(FormatOptionItem? value)
        {
            OnPropertyChanged(nameof(CanFlip));
            OnPropertyChanged(nameof(CanShortMonth));
            OnPropertyChanged(nameof(FlipText));
            Apply();
        }

        partial void OnIsFlippedChanged(bool value) => OnModifierChanged();

        partial void OnUseShortMonthChanged(bool value) => OnModifierChanged();

        partial void OnPrefixChanged(string value) => Apply();

        partial void OnSuffixChanged(string value) => Apply();

        private void OnModifierChanged()
        {
            OnPropertyChanged(nameof(FlipText));
            if (_loading || SelectedOption == null) return;
            // the names in the list show the modifiers, so the list is rebuilt
            _loading = true;
            RebuildOptions(SelectedOption.Core);
            _loading = false;
            Apply();
        }

        [RelayCommand]
        private void ToggleFlip()
        {
            if (CanFlip) IsFlipped = !IsFlipped;
        }

        private void Apply()
        {
            UpdatePreview();
            if (_loading || SelectedOption == null) return;
            _state.Settings.FolderFormat = DateFormatChoice.Resolve(SelectedOption.Core, IsFlipped, UseShortMonth);
            _state.Settings.FolderPrefix = AppConstants.SanitizeFolderName(Prefix);
            _state.Settings.FolderSuffix = AppConstants.SanitizeFolderName(Suffix);
            _state.Save();
        }

        private void UpdatePreview()
        {
            if (SelectedOption == null) return;
            var format = DateFormatChoice.Resolve(SelectedOption.Core, IsFlipped, UseShortMonth);
            Samples = DateFormatChoice.SampleDates(SelectedOption.Core, DateTime.Now.Year)
                .Select(d => AppConstants.FormatFolderDate(d, format, Prefix, Suffix))
                .ToList();
        }
    }
}
