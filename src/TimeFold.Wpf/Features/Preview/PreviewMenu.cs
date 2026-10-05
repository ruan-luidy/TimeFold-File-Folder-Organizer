using TimeFold.Core.Exclusions;
using TimeFold.Core.FileTypes;
using TimeFold.Core.Settings;

namespace TimeFold.Wpf.Features.Preview
{
    /// <summary>
    /// Which context menu entries the right-clicked row gets, and their texts
    /// (the Opening handler of MainForm.ContextMenu).
    /// </summary>
    public sealed class PreviewMenu
    {
        public PreviewMenu(PreviewRow? row, AppSettings settings)
        {
            var item = row?.Item;
            HasItem = item != null;
            bool isDirectory = item is { IsDirectory: true };
            bool isFileWithExt = item != null && !isDirectory && !string.IsNullOrEmpty(item.Extension);
            bool isCatMode = settings.OrgMode is OrganizationMode.Category or OrganizationMode.CategoryAndDate or OrganizationMode.DateAndCategory;

            ShowIgnore = isDirectory;
            if (isDirectory)
            {
                bool isIgnored = IgnoreMarker.IsIgnored(item!.FullPath) || item.IsExcludedByRule;
                IgnoreText = isIgnored ? $"Stop ignoring folder '{item.Name}'" : $"Ignore folder '{item.Name}'...";
            }

            ShowRename = HasItem && !isDirectory;

            string currentCategory = isFileWithExt ? FileTypeService.Instance.GetCategory(item!.Extension) : "";
            string originalName = "";
            bool isRenamed = isFileWithExt && isCatMode && FileTypeService.Instance.IsCategoryRenamed(currentCategory, out originalName);

            ShowRenameCategory = isFileWithExt && isCatMode;
            RenameCategoryText = $"Rename Category '{currentCategory}'...";
            ShowResetCategory = isRenamed;
            ResetCategoryText = $"Revert Category to Factory Name ('{originalName}')";

            ShowChangeCategory = (isFileWithExt || isDirectory) && isCatMode;
            if (isDirectory)
            {
                bool isGit = item!.IsGitRepository && settings.GroupGitRepositories;
                ChangeCategoryText = isGit ? "Set target folder for all Git repositories..." : "Set target folder for all folders...";
            }
            else if (isFileWithExt)
            {
                ChangeCategoryText = $"Set target folder for all {item!.Extension.ToLowerInvariant()} files...";
            }

            ShowCategorySection = ShowRenameCategory || ShowChangeCategory;
        }

        public bool HasItem { get; }

        public bool ShowIgnore { get; }

        public string IgnoreText { get; } = string.Empty;

        public bool ShowRename { get; }

        public bool ShowCategorySection { get; }

        public bool ShowRenameCategory { get; }

        public string RenameCategoryText { get; }

        public bool ShowResetCategory { get; }

        public string ResetCategoryText { get; }

        public bool ShowChangeCategory { get; }

        public string ChangeCategoryText { get; } = string.Empty;
    }
}
