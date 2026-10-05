using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeFold.Core.Config;
using TimeFold.Core.Conflicts;
using TimeFold.Core.Files;
using TimeFold.Core.FileTypes;
using TimeFold.Core.Settings;

namespace TimeFold.Wpf.Features.Preview
{
    public enum RowStatus
    {
        Ready,
        Conflict,
        Excluded,
        Unchecked,
    }

    public enum ItemKind
    {
        File,
        Folder,
        GitRepo,
        IgnoredFolder,
    }

    /// <summary>
    /// One line of the preview: the FileItem plus what the WinForms list painted by hand per cell
    /// (the active date with a check, the custom route colour, the status column).
    /// </summary>
    public sealed partial class PreviewRow : ObservableObject
    {
        private readonly PreviewViewModel _owner;

        public PreviewRow(FileItem item, PreviewViewModel owner)
        {
            Item = item;
            _owner = owner;
        }

        public FileItem Item { get; }

        private AppSettings Settings => _owner.Settings;

        private ConflictInfo? Conflict => _owner.ConflictFor(Item);

        public string Name => Item.Name;

        public string TypeDisplay => Item.TypeDisplay.Replace("📁 ", string.Empty);

        public ItemKind Kind => !Item.IsDirectory ? ItemKind.File
            : Item.IsExcludedByRule ? ItemKind.IgnoredFolder
            : Item.IsGitRepository ? ItemKind.GitRepo
            : ItemKind.Folder;

        public string? TypeTooltip => Item.IsGitRepository
            ? "📦 Git Repository (Folder)\nFolder containing a .git or .github repository structure.\n• Kept intact as a whole folder (contents are never touched).\n• In Date mode, organized into date timeline folder.\n• In Category modes, isolated into 'Git Repos'."
            : null;

        // Checking an excluded folder is refused by the view model, which tells the user why
        public bool IsChecked
        {
            get => Item.IsSelected && !Item.IsExcludedByRule;
            set
            {
                if (value == IsChecked) return;
                _owner.SetChecked(this, value);
            }
        }

        public bool IsMuted => Item.IsExcludedByRule || !Item.IsSelected;

        private bool IsMediaActive => !IsMuted && Item.IsMediaDateActive && Item.MediaDateTaken.HasValue;

        public bool IsModifiedActive => !IsMuted && !Item.IsCreatedDateActive && !IsMediaActive;

        public bool IsCreatedActive => !IsMuted && Item.IsCreatedDateActive && !IsMediaActive;

        public bool IsTakenActive => IsMediaActive;

        // compact in the cell so three date columns fit; the tooltip keeps the full format of the WinForms list
        public string Modified => ShortDate(Item.ModifiedDate);

        public string Created => ShortDate(Item.CreatedDate);

        public string Taken => Item.MediaDateTaken.HasValue ? ShortDate(Item.MediaDateTaken.Value) : "—";

        private static string ShortDate(DateTime date) => date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        public string DateTooltip => AppConstants.BuildDateTooltip(Item);

        public bool IsCustomRoute => !Item.IsDirectory && Settings.OrgMode != OrganizationMode.Date && FileTypeService.Instance.IsCustomRoute(Item.Extension);

        public string Target => Item.IsExcludedByRule ? "— (Ignored)"
            : !Item.IsSelected ? "— (Skipped)"
            : IsCustomRoute ? $"{Item.TargetFolder} (Custom)"
            : Item.TargetFolder;

        public string TargetTooltip
        {
            get
            {
                string outputBase = _owner.OutputBase;
                string subPath = Settings.CreateSortedSubfolder
                    ? Path.Combine(AppConstants.GetSortedFolderPreviewPattern(Settings.Use24HourTimestamp), Item.TargetFolder)
                    : Item.TargetFolder;
                string notice = Settings.CreateSortedSubfolder
                    ? "✔ 'Create Sorted Subfolder' is ON:\nIsolates files inside a clean timestamped folder."
                    : "ℹ 'Create Sorted Subfolder' is OFF:\nFiles will be placed directly in output folder.";
                if (IsCustomRoute)
                {
                    notice += $"\n\n🏷️ Custom Route: '{Item.Extension.ToLowerInvariant()}' ➔ '{Item.TargetFolder}'\nTip: Right-click this file to change or reset destination.";
                }
                return $"📁 Planned Destination:\n{Path.Combine(outputBase, subPath)}\n\n{notice}";
            }
        }

        public RowStatus Status => Item.IsExcludedByRule ? RowStatus.Excluded
            : !Item.IsSelected ? RowStatus.Unchecked
            : Conflict != null ? RowStatus.Conflict
            : RowStatus.Ready;

        public string StatusText => Status switch
        {
            RowStatus.Excluded => "Excluded (Rule)",
            RowStatus.Unchecked => "Unchecked",
            RowStatus.Conflict => Conflict!.ShortStatus.Replace("⚠ ", string.Empty),
            _ => "Ready",
        };

        public string? StatusTooltip => Conflict is { } c ? $"{c.Description}\n{c.ProposedAction}" : null;

        public string Size => Item.IsDirectory ? "—" : FormatFileSize(Item.Size);

        /// <summary>Selection, conflicts or routes changed: every computed column is read again.</summary>
        public void Refresh() => OnPropertyChanged(string.Empty);

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
