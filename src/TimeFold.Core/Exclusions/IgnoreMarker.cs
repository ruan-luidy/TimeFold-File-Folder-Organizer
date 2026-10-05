using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TimeFold.Core.Config;

namespace TimeFold.Core.Exclusions
{
    /// <summary>
    /// The .timefold-ignore marker file: a folder that has one is skipped by the scan.
    /// Moved out of MainForm.ToggleFolderIgnore and FolderExclusionDialog so the WPF side only asks and refreshes.
    /// </summary>
    public static class IgnoreMarker
    {
        public static string MarkerPath(string folderPath) =>
            Path.Combine(folderPath, AppConstants.TimefoldIgnoreFileName);

        public static bool IsIgnored(string folderPath) => File.Exists(MarkerPath(folderPath));

        public static void Add(string folderPath)
        {
            string markerPath = MarkerPath(folderPath);
            if (File.Exists(markerPath)) return;
            File.WriteAllText(markerPath, AppConstants.TimefoldIgnoreFileContent);
            try { File.SetAttributes(markerPath, FileAttributes.Hidden); } catch { }
        }

        public static void Remove(string folderPath)
        {
            string markerPath = MarkerPath(folderPath);
            if (!File.Exists(markerPath)) return;
            try { File.SetAttributes(markerPath, FileAttributes.Normal); } catch { }
            File.Delete(markerPath);
        }

        /// <summary>Names of the direct subfolders of <paramref name="sourceFolder"/> that carry a marker.</summary>
        public static List<string> ListIgnored(string? sourceFolder)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(sourceFolder) && Directory.Exists(sourceFolder))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(sourceFolder))
                    {
                        if (IsIgnored(dir))
                            names.Add(Path.GetFileName(dir));
                    }
                }
                catch { }
            }
            return names.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Leaves a marker in exactly the subfolders named in <paramref name="names"/> and removes it from the rest
        /// (same as Save &amp; Apply in the old exclusion dialog, including dropping a marker on the source root).
        /// </summary>
        public static void Apply(string sourceFolder, IEnumerable<string> names)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder)) return;

            var set = new HashSet<string>(
                names.Select(n => n.Trim().TrimEnd('/', '\\')).Where(n => n.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            try
            {
                Remove(sourceFolder);
                foreach (var dir in Directory.GetDirectories(sourceFolder))
                {
                    if (set.Contains(Path.GetFileName(dir))) Add(dir);
                    else Remove(dir);
                }
            }
            catch { }
        }

        public static int CountMarkedFolders(IEnumerable<Files.FileItem> items) =>
            items.Count(f => f.IsDirectory && IsIgnored(f.FullPath));
    }
}
