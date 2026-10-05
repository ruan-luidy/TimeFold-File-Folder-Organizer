using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TimeFold.Core.Config;
using TimeFold.Core.FileTypes;
using TimeFold.Core.Settings;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.FileTypes
{
    public enum RuleStatus { FactoryDefault, Disabled, UserOverride, CustomRule }

    /// <summary>One extension of the rules table; unticking disables a factory extension.</summary>
    public sealed partial class TypeRuleRow : ObservableObject
    {
        private readonly FileTypesViewModel _owner;
        private bool _isEnabled;

        public TypeRuleRow(FileTypesViewModel owner, string extension, string category, RuleStatus status)
        {
            _owner = owner;
            Extension = extension;
            Category = category;
            Status = status;
            _isEnabled = status != RuleStatus.Disabled;
        }

        public string Extension { get; }

        public string Category { get; }

        public RuleStatus Status { get; private set; }

        public string StatusText => Status switch
        {
            RuleStatus.UserOverride => "User Override",
            RuleStatus.Disabled => "Disabled",
            RuleStatus.CustomRule => "Custom User Rule",
            _ => "Factory Default",
        };

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (!SetProperty(ref _isEnabled, value)) return;
                if (Status is RuleStatus.FactoryDefault or RuleStatus.Disabled)
                {
                    Status = value ? RuleStatus.FactoryDefault : RuleStatus.Disabled;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusText));
                }
                _owner.SetExtensionEnabled(Extension, value);
            }
        }
    }

    /// <summary>
    /// File type and category rules (TypeRulesDialog) and smart companion pairing (SmartPairingDialog).
    /// Every change is saved at once, as in the old dialog.
    /// </summary>
    public sealed partial class FileTypesViewModel : ObservableObject
    {
        private readonly AppState _state;
        private readonly FileTypeService _service = FileTypeService.Instance;
        private bool _loading;

        [ObservableProperty]
        private string _search = string.Empty;

        [ObservableProperty]
        private string _categoryPrefix = string.Empty;

        [ObservableProperty]
        private string _categorySuffix = string.Empty;

        [ObservableProperty]
        private string _newExtension = string.Empty;

        [ObservableProperty]
        private string _newCategory = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedHasOverride), nameof(SelectedCategoryRenamed), nameof(RenameCategoryText), nameof(ResetCategoryText), nameof(RemapText))]
        private TypeRuleRow? _selectedRule;

        public FileTypesViewModel(AppState state)
        {
            _state = state;
            Load();
        }

        public ObservableCollection<TypeRuleRow> Rules { get; } = new();

        public ObservableCollection<string> Categories { get; } = new();

        public int MaxLength => AppConstants.MaxPrefixSuffixLength;

        public bool KeepSubtitleCompanionsTogether
        {
            get => _state.Settings.KeepSubtitleCompanionsTogether;
            set
            {
                _state.Settings.KeepSubtitleCompanionsTogether = value;
                OnPropertyChanged();
                _state.Save();
            }
        }

        public bool KeepHtmlCompanionsTogether
        {
            get => _state.Settings.KeepHtmlCompanionsTogether;
            set
            {
                _state.Settings.KeepHtmlCompanionsTogether = value;
                OnPropertyChanged();
                _state.Save();
            }
        }

        public bool SelectedHasOverride => SelectedRule != null && _service.Delta.ExtensionCategoryOverrides.ContainsKey(SelectedRule.Extension);

        public bool SelectedCategoryRenamed => SelectedRule != null && _service.IsCategoryRenamed(_service.GetCategory(SelectedRule.Extension), out _);

        public string RenameCategoryText => SelectedRule == null ? string.Empty : $"Rename Category '{_service.GetCategory(SelectedRule.Extension)}'...";

        public string ResetCategoryText
        {
            get
            {
                if (SelectedRule == null) return string.Empty;
                _service.IsCategoryRenamed(_service.GetCategory(SelectedRule.Extension), out var original);
                return $"Revert Category to Factory Name ('{original}')";
            }
        }

        public string RemapText => SelectedRule == null ? string.Empty : $"Remap {SelectedRule.Extension} to Another Category...";

        public void Load()
        {
            _loading = true;
            CategoryPrefix = _state.Settings.CategoryPrefix;
            CategorySuffix = _state.Settings.CategorySuffix;
            _loading = false;
            OnPropertyChanged(nameof(KeepSubtitleCompanionsTogether));
            OnPropertyChanged(nameof(KeepHtmlCompanionsTogether));
            LoadRulesList();
        }

        partial void OnSearchChanged(string value) => LoadRulesList();

        partial void OnCategoryPrefixChanged(string value) => OnPrefixSuffixChanged();

        partial void OnCategorySuffixChanged(string value) => OnPrefixSuffixChanged();

        private void OnPrefixSuffixChanged()
        {
            if (_loading) return;
            _state.Settings.CategoryPrefix = AppConstants.SanitizeFolderName(CategoryPrefix);
            _state.Settings.CategorySuffix = AppConstants.SanitizeFolderName(CategorySuffix);
            _state.Save();
            LoadRulesList();
        }

        /// <summary>Same order as the old list: user overrides first, then alphabetical.</summary>
        private void LoadRulesList()
        {
            Categories.Clear();
            foreach (var cat in _service.GetAllCategories()) Categories.Add(cat);
            if (string.IsNullOrWhiteSpace(NewCategory) && Categories.Count > 0) NewCategory = Categories[0];

            var allExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var extList in FileTypeService.FactoryCategories.Values)
                foreach (var ext in extList) allExtensions.Add(ext);
            foreach (var ext in _service.Delta.ExtensionCategoryOverrides.Keys) allExtensions.Add(ext);
            foreach (var extList in _service.Delta.CustomCategories.Values)
                foreach (var ext in extList) allExtensions.Add(ext);

            string query = Search.Trim().ToLowerInvariant();
            var sortedExts = allExtensions
                .Where(ext => !ext.StartsWith("::", StringComparison.Ordinal))
                .OrderByDescending(ext => _service.Delta.ExtensionCategoryOverrides.ContainsKey(ext))
                .ThenBy(ext => ext);

            Rules.Clear();
            foreach (var ext in sortedExts)
            {
                bool isFactory = FileTypeService.FactoryCategories.Values.Any(list => list.Contains(ext, StringComparer.OrdinalIgnoreCase));
                bool isDisabled = _service.Delta.DisabledFactoryExtensions.Contains(ext);
                bool isOverride = _service.Delta.ExtensionCategoryOverrides.ContainsKey(ext);
                string currentCat = _service.GetCategory(ext);

                if (!string.IsNullOrEmpty(query) && !ext.ToLowerInvariant().Contains(query) && !currentCat.ToLowerInvariant().Contains(query))
                    continue;

                var status = isOverride ? RuleStatus.UserOverride : (isFactory ? (isDisabled ? RuleStatus.Disabled : RuleStatus.FactoryDefault) : RuleStatus.CustomRule);
                string displayCat = AppConstants.FormatCategoryFolder(currentCat, _state.Settings.CategoryPrefix, _state.Settings.CategorySuffix);
                if (isOverride) displayCat += " (Custom)";
                Rules.Add(new TypeRuleRow(this, ext, displayCat, status));
            }
        }

        public void SetExtensionEnabled(string ext, bool enabled)
        {
            if (enabled) _service.Delta.DisabledFactoryExtensions.Remove(ext);
            else _service.Delta.DisabledFactoryExtensions.Add(ext);
            _service.SaveDelta();
            _service.RebuildLookupTable();
            _state.Save();
        }

        [RelayCommand]
        private async Task Assign()
        {
            string ext = NewExtension.Trim();
            string cat = NewCategory.Trim();
            if (string.IsNullOrWhiteSpace(ext) || string.IsNullOrWhiteSpace(cat))
            {
                await DialogService.WarningAsync("Invalid Input", "Please enter an extension and category.");
                return;
            }

            if (!ext.StartsWith('.')) ext = "." + ext;
            _service.Delta.ExtensionCategoryOverrides[ext] = cat;
            _service.Delta.DisabledFactoryExtensions.Remove(ext);
            _service.SaveDelta();
            _service.RebuildLookupTable();
            NewExtension = string.Empty;
            LoadRulesList();
            _state.Save();
        }

        [RelayCommand]
        private async Task ResetToDefaults()
        {
            bool confirmed = await DialogService.ConfirmAsync("Confirm Reset", "Reset all file type and category rules back to built-in factory defaults?", "Reset");
            if (!confirmed) return;

            _service.ResetToFactoryDefaults();
            _state.Settings.OrgMode = OrganizationMode.Date;
            _state.Settings.CategoryPrefix = AppConstants.DefaultCategoryPrefix;
            _state.Settings.CategorySuffix = AppConstants.DefaultCategorySuffix;
            _state.Save();
            Load();
        }

        [RelayCommand]
        private async Task Export()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export File Type Rules",
                Filter = "JSON Files (*.json)|*.json",
                FileName = "timefold_custom_types.json"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                _service.ExportRules(dialog.FileName);
                await DialogService.SuccessAsync("Export", "Rules exported successfully!");
            }
            catch (Exception ex)
            {
                await DialogService.ErrorAsync("Error", $"Export failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Import()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import File Type Rules",
                Filter = "JSON Files (*.json)|*.json"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                _service.ImportRules(dialog.FileName);
                LoadRulesList();
                _state.Save();
                await DialogService.SuccessAsync("Import", "Rules imported successfully!");
            }
            catch (Exception ex)
            {
                await DialogService.ErrorAsync("Error", $"Import failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void RestorePairingDefaults()
        {
            KeepSubtitleCompanionsTogether = AppConstants.DefaultKeepSubtitleCompanionsTogether;
            KeepHtmlCompanionsTogether = AppConstants.DefaultKeepHtmlCompanionsTogether;
        }

        [RelayCommand]
        private async Task RenameSelectedCategory()
        {
            if (SelectedRule == null) return;
            string currentCat = _service.GetCategory(SelectedRule.Extension);
            string? newName = await DialogService.PromptAsync("Rename Category", $"Enter new name for category '{currentCat}':", currentCat);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                _service.RenameCategory(currentCat, newName);
                LoadRulesList();
                _state.Save();
            }
        }

        [RelayCommand]
        private void ResetSelectedCategoryName()
        {
            if (SelectedRule == null) return;
            if (_service.ResetCategoryName(_service.GetCategory(SelectedRule.Extension)))
            {
                LoadRulesList();
                _state.Save();
            }
        }

        [RelayCommand]
        private void RemapSelectedExtension()
        {
            if (SelectedRule == null) return;
            NewExtension = SelectedRule.Extension;
            NewCategory = _service.GetCategory(SelectedRule.Extension);
            RemapRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void ResetSelectedExtensionOverride()
        {
            if (SelectedRule == null) return;
            _service.RemoveCategoryOverride(SelectedRule.Extension);
            LoadRulesList();
            _state.Save();
        }

        /// <summary>The page moves the focus to the category field.</summary>
        public event EventHandler? RemapRequested;
    }
}
