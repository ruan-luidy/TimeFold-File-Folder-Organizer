using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using TimeFold.Core.Files;
using TimeFold.Core.Settings;

namespace TimeFold.Core.Config
{
    /// <summary>
    /// Single Source of Truth (SSoT) for application metadata, default preferences, and naming rules.
    /// </summary>
    public static class AppConstants
    {
        // Application Metadata (SSoT)
        // NOTE: AppVersion comes from the app assembly (<Version>x.y.z</Version> in TimeFold.Wpf.csproj).
        public const string VendorName = "Appsphinx";
        public const string AppName = "TimeFold: File & Folder Organizer";
        public const string ShortAppName = "TimeFold";

        // The entry assembly is the app; the tests fall back to Core
        private static Assembly AppAssembly => Assembly.GetEntryAssembly() ?? typeof(AppConstants).Assembly;

        public static string AppVersion => AppAssembly.GetName().Version!.ToString(3);
        private static readonly Lazy<string> _lazyBuildNumber = new(() =>
        {
            try
            {
                foreach (var attr in AppAssembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                {
                    if (attr.Key == "BuildTimestamp" && !string.IsNullOrWhiteSpace(attr.Value))
                    {
                        return attr.Value;
                    }
                }
            }
            catch { }
            return "20260921";
        });

        public static string BuildNumber => _lazyBuildNumber.Value;
        public const string AppTagline = "Effortlessly organize files & folders into clean date-based timelines or smart categories";
        public const string AppDescription = "Fast, non-destructive file and folder organizer for Windows that sorts messy directories into clean date-based timelines or smart file-type categories.";
        public const string Author = "Shree";
        public const string RepositoryUrl = "https://github.com/chandrath/TimeFold-File-Folder-Organizer";
        // GitHub Releases — derived from RepositoryUrl, no personal credentials
        public const string ReleasesApiUrl = "https://api.github.com/repos/chandrath/TimeFold-File-Folder-Organizer/releases/latest";
        public const string ReleasesPageUrl = RepositoryUrl + "/releases/latest";
        public const string ReleaseSummaryStart = "<!-- timefold-summary-start -->";
        public const string ReleaseSummaryEnd = "<!-- timefold-summary-end -->";
        public const string LicenseText = "GNU General Public License v3.0 (GPLv3) - Free and Open Source";
        public const string CopyrightText = LicenseText;

        // Configuration File Paths (SSoT)
        // TIMEFOLD_CONFIG_DIR points the whole config elsewhere, so the tests never touch the real settings.
        public static string GetConfigDirectoryPath() =>
            Environment.GetEnvironmentVariable("TIMEFOLD_CONFIG_DIR") is { Length: > 0 } custom
                ? custom
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), VendorName, ShortAppName);

        public static string GetLegacyConfigDirectoryPath() =>
            Environment.GetEnvironmentVariable("TIMEFOLD_CONFIG_DIR") is { Length: > 0 } custom
                ? Path.Combine(custom, "legacy")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ShortAppName);

        public static string GetConfigFilePath() =>
            Path.Combine(GetConfigDirectoryPath(), "settings.json");

        public static string GetCustomTypesFilePath() =>
            Path.Combine(GetConfigDirectoryPath(), "custom_types.json");

        public static string GetUndoManifestFilePath() =>
            Path.Combine(GetConfigDirectoryPath(), "last_undo.json");

        public const int UndoSessionMaxAgeDays = 7;

        /// <summary>
        /// Writes and deletes a probe file. The WinForms version showed the MessageBox from here;
        /// now the caller decides how to tell the user.
        /// </summary>
        public static bool TryWriteProbe(string targetDir, out Exception? error)
        {
            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                string testFile = Path.Combine(targetDir, $".tf_perm_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        // Output & Logging Prefixes (SSoT)
        public const string SortedFolderPrefix = "Sorted_";
        public const string DefaultGroupedFolderName = "Grouped Folders";
        public const string DefaultGitReposFolderName = "Git Repos";
        public const string CsvLogPrefix = ShortAppName + "_Log_";
        public const string LegacyCsvLogPrefix = "OrganizationLog_";

        // Default User Preferences
        public const bool DefaultIncludeTopLevelFolders = true;
        public const bool DefaultGroupGitRepositories = true;
        public const bool DefaultIgnoreSystemFiles = true;
        public const bool DefaultShowDetailedProgress = true;
        public const bool DefaultShowOnTop = false;
        public const bool DefaultGenerateCsvLog = true;
        public const bool DefaultUse24HourTimestamp = false;
        public const bool DefaultAutoLoadExeDirectoryOnStartup = false;
        public const bool DefaultDarkMode = false;
        public const bool DefaultHasSeenWelcomeTour = false;
        public const bool DefaultCreateSortedSubfolder = true;
        public const bool DefaultUseSourceAsOutput = true;
        public const bool DefaultEnableFolderExclusions = true;
        public const bool DefaultShowIgnoreMarkerWarning = true;
        public const string TimefoldIgnoreFileName = ".timefold-ignore";
        public static string TimefoldIgnoreFileContent =>
            $"# TimeFold Folder Ignore Marker\r\n" +
            $"# Created by {AppName} ({RepositoryUrl})\r\n" +
            $"#\r\n" +
            $"# This file tells TimeFold to skip organizing this folder and its contents.\r\n" +
            $"# You can safely delete this file anytime if you want TimeFold to organize this folder again.\r\n";
        public const int MaxRecentFolders = 5;
        public const FolderFormat DefaultFolderFormat = FolderFormat.YearMonth;
        public const string DefaultFolderPrefix = "";
        public const string DefaultFolderSuffix = "";
        public const string DefaultCategoryPrefix = "";
        public const string DefaultCategorySuffix = "";
        public const int MaxPrefixSuffixLength = 30;
        public const bool DefaultKeepHtmlCompanionsTogether = true;
        public const bool DefaultKeepSubtitleCompanionsTogether = true;
        public const bool DefaultPackageVideoSubtitles = true;
        public const string FolderCategoryKey = "::folder::";
        public const string GitRepoCategoryKey = "::git-repo::";
        public const DateSource DefaultFileDateSource = DateSource.Modified;
        public const DateSource DefaultFolderDateSource = DateSource.Modified;
        public const bool DefaultUseMediaDateTaken = true;
        public const string MediaDateTakenSettingLabel = "Prioritize original media Date Taken (EXIF / Camera timestamp)";
        public const string MediaDateTakenSettingTooltip = "When checked, extracts original capture/creation dates from photo EXIF and video metadata (JPEG, MP4, HEIC, MOV, PNG) to organize media by when it was taken.\n\nWhen unchecked, media files follow your selected Date Modified or Date Created source instead.";
        public const string TooltipDateFormat = "dd MMM yyyy hh:mm:ss tt";
        public static string FormatTooltipDate(DateTime dt) => dt.ToString(TooltipDateFormat, CultureInfo.InvariantCulture);
        public static string FormatDisplayDate(DateTime dt) => dt.ToString(TooltipDateFormat, CultureInfo.InvariantCulture);

        public static string BuildDateTooltip(FileItem file)
        {
            var sb = new System.Text.StringBuilder();
            if (file.IsMediaDateActive && file.MediaDateTaken.HasValue)
            {
                sb.AppendLine($"📷 Date Taken:    {FormatTooltipDate(file.MediaDateTaken.Value)}  ✔ (Active)");
                sb.AppendLine($"📅 Date Modified: {FormatTooltipDate(file.ModifiedDate)}");
                sb.AppendLine($"📁 Date Created:  {FormatTooltipDate(file.CreatedDate)}");
                sb.AppendLine();
                sb.AppendLine($"Used to organize this item into target folder '{file.TargetFolder}'.");
                sb.Append($"Tip: To use Modified/Created date instead for media as well, disable '{MediaDateTakenSettingLabel}' in Settings > Preferences.");
            }
            else
            {
                string modActive = !file.IsCreatedDateActive ? "  ✔ (Active)" : "";
                string creActive = file.IsCreatedDateActive ? "  ✔ (Active)" : "";
                sb.AppendLine($"📅 Date Modified: {FormatTooltipDate(file.ModifiedDate)}{modActive}");
                sb.AppendLine($"📁 Date Created:  {FormatTooltipDate(file.CreatedDate)}{creActive}");
                sb.AppendLine();
                sb.AppendLine($"Used to organize this item into target folder '{file.TargetFolder}'.");
                sb.Append("Tip: Change date source rules in Settings > Preferences");
            }
            return sb.ToString();
        }
        public const int DefaultMaxPreviewItems = 1000;
        public const int MaxAllowedPreviewItems = 100000;
        public const int MinAllowedPreviewItems = 100;

        // Detection Thresholds
        public const double TimestampSimilarityThreshold = 0.85; // 85%

        public static string GetSortedFolderName(string outputDirectory, bool use24Hour, DateTime? now = null)
        {
            var dt = now ?? DateTime.Now;
            string timestamp = use24Hour
                ? dt.ToString("yyyy-MM-dd_HH-mm")
                : dt.ToString("yyyy-MM-dd_hh-mmtt");

            string baseFolder = Path.Combine(outputDirectory, $"{SortedFolderPrefix}{timestamp}");
            string folder = baseFolder;
            int counter = 1;
            while (Directory.Exists(folder))
            {
                folder = $"{baseFolder} ({counter++})";
            }
            return folder;
        }

        public static string GetSortedFolderPreviewPattern(bool use24Hour)
        {
            return use24Hour
                ? $"{SortedFolderPrefix}YYYY-MM-DD_HH-mm"
                : $"{SortedFolderPrefix}YYYY-MM-DD_hh-mmtt";
        }

        public static string GetSortedFolderPreviewPath(string outputDirectory, bool use24Hour)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                return string.Empty;

            return Path.Combine(outputDirectory, GetSortedFolderPreviewPattern(use24Hour));
        }

        // Known Windows System Files and Protected Directories
        public static readonly HashSet<string> KnownSystemFilesAndDirs = new(StringComparer.OrdinalIgnoreCase)
        {
            "desktop.ini",
            "thumbs.db",
            "ehthumbs.db",
            "ehthumbs_vista.db",
            "$recycle.bin",
            "system volume information"
        };

        public static string SanitizeFolderName(string input, int maxLength = MaxPrefixSuffixLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new System.Text.StringBuilder(Math.Min(input.Length, maxLength));
            foreach (char c in input)
            {
                if (Array.IndexOf(invalidChars, c) < 0)
                {
                    sanitized.Append(c);
                    if (sanitized.Length >= maxLength)
                        break;
                }
            }
            return sanitized.ToString();
        }

        private static readonly string[] QuarterMonthsFull =
        [
            "January, February & March",
            "April, May & June",
            "July, August & September",
            "October, November & December"
        ];

        private static readonly string[] QuarterMonthsShort =
        [
            "Jan, Feb & Mar",
            "Apr, May & Jun",
            "Jul, Aug & Sep",
            "Oct, Nov & Dec"
        ];

        public static string FormatFolderDate(DateTime date, FolderFormat format, string prefix = "", string suffix = "")
        {
            int year = date.Year;
            int month = date.Month;
            int day = date.Day;
            string monthFull = date.ToString("MMMM", CultureInfo.InvariantCulture);
            string monthShort = date.ToString("MMM", CultureInfo.InvariantCulture);
            int quarter = (month - 1) / 3 + 1;
            int half = (month <= 6) ? 1 : 2;

            string core = format switch
            {
                FolderFormat.YearMonth => $"{year} {monthFull}",
                FolderFormat.MonthYear => $"{monthFull} {year}",
                FolderFormat.YearShortMonth => $"{year} {monthShort}",
                FolderFormat.ShortMonthYear => $"{monthShort} {year}",
                FolderFormat.IsoMonth => $"{year}-{month:D2}",
                FolderFormat.MonthIso => $"{month:D2}-{year}",
                FolderFormat.IsoDate => $"{year}-{month:D2}-{day:D2}",
                FolderFormat.YearMonthDay => $"{year} {monthFull} {day}",
                FolderFormat.DayMonthYear => $"{day} {monthFull} {year}",
                FolderFormat.YearShortMonthDay => $"{year} {monthShort} {day}",
                FolderFormat.DayShortMonthYear => $"{day} {monthShort} {year}",
                FolderFormat.YearQuarter => $"{year} Q{quarter}",
                FolderFormat.QuarterYear => $"Q{quarter} {year}",
                FolderFormat.YearQuarterMonths => $"{year} Q{quarter} ({QuarterMonthsFull[quarter - 1]})",
                FolderFormat.YearQuarterShortMonths => $"{year} Q{quarter} ({QuarterMonthsShort[quarter - 1]})",
                FolderFormat.YearHalf => $"{year} H{half}",
                FolderFormat.HalfYear => $"H{half} {year}",
                FolderFormat.YearOnly => $"{year}",
                // Year-nested: returns "year\subfolder" path
                FolderFormat.YearWithMonth => System.IO.Path.Combine($"{year}", $"{year} {monthFull}"),
                FolderFormat.YearWithShortMonth => System.IO.Path.Combine($"{year}", $"{year} {monthShort}"),
                FolderFormat.YearWithMonthFlipped => System.IO.Path.Combine($"{year}", $"{monthFull} {year}"),
                FolderFormat.YearWithShortMonthFlipped => System.IO.Path.Combine($"{year}", $"{monthShort} {year}"),
                FolderFormat.YearWithMonthOnly => System.IO.Path.Combine($"{year}", $"{monthFull}"),
                FolderFormat.YearWithShortMonthOnly => System.IO.Path.Combine($"{year}", $"{monthShort}"),
                FolderFormat.YearWithIsoMonth => System.IO.Path.Combine($"{year}", $"{year}-{month:D2}"),
                FolderFormat.YearWithIsoMonthFlipped => System.IO.Path.Combine($"{year}", $"{month:D2}-{year}"),
                FolderFormat.YearWithQuarter => System.IO.Path.Combine($"{year}", $"{year} Q{quarter}"),
                FolderFormat.YearWithQuarterFlipped => System.IO.Path.Combine($"{year}", $"Q{quarter} {year}"),
                FolderFormat.YearWithHalf => System.IO.Path.Combine($"{year}", $"{year} H{half}"),
                FolderFormat.YearWithHalfFlipped => System.IO.Path.Combine($"{year}", $"H{half} {year}"),
                FolderFormat.IsoDateFlipped => $"{day:D2}-{month:D2}-{year}",
                FolderFormat.YearWithMonthAndDay => System.IO.Path.Combine($"{year}", $"{monthFull}", $"{day:D2}"),
                FolderFormat.YearWithShortMonthAndDay => System.IO.Path.Combine($"{year}", $"{monthShort}", $"{day:D2}"),
                FolderFormat.YearWithMonthAndDayFlipped => System.IO.Path.Combine($"{day:D2}", $"{monthFull}", $"{year}"),
                FolderFormat.YearWithShortMonthAndDayFlipped => System.IO.Path.Combine($"{day:D2}", $"{monthShort}", $"{year}"),
                FolderFormat.YearWithIsoMonthAndDay => System.IO.Path.Combine($"{year}", $"{year}-{month:D2}", $"{day:D2}"),
                FolderFormat.YearWithIsoMonthAndDayFlipped => System.IO.Path.Combine($"{day:D2}", $"{year}-{month:D2}", $"{year}"),
                _ => $"{year} {monthFull}"
            };

            string cleanPrefix = SanitizeFolderName(prefix);
            string cleanSuffix = SanitizeFolderName(suffix);

            if (!string.IsNullOrEmpty(cleanPrefix) && !cleanPrefix.EndsWith(" ") && !cleanPrefix.EndsWith("_") && !cleanPrefix.EndsWith("-"))
            {
                cleanPrefix += " ";
            }

            if (!string.IsNullOrEmpty(cleanSuffix) && !cleanSuffix.StartsWith(" ") && !cleanSuffix.StartsWith("_") && !cleanSuffix.StartsWith("-"))
            {
                cleanSuffix = " " + cleanSuffix;
            }

            return $"{cleanPrefix}{core}{cleanSuffix}";
        }

        public static string FormatCategoryFolder(string category, string prefix = "", string suffix = "")
        {
            if (string.IsNullOrWhiteSpace(category)) category = DefaultGroupedFolderName;
            string cleanPrefix = SanitizeFolderName(prefix);
            string cleanSuffix = SanitizeFolderName(suffix);

            if (!string.IsNullOrEmpty(cleanPrefix) && !cleanPrefix.EndsWith(" ") && !cleanPrefix.EndsWith("_") && !cleanPrefix.EndsWith("-"))
            {
                cleanPrefix += " ";
            }

            if (!string.IsNullOrEmpty(cleanSuffix) && !cleanSuffix.StartsWith(" ") && !cleanSuffix.StartsWith("_") && !cleanSuffix.StartsWith("-"))
            {
                cleanSuffix = " " + cleanSuffix;
            }

            return $"{cleanPrefix}{category}{cleanSuffix}".Trim();
        }
    }
}
