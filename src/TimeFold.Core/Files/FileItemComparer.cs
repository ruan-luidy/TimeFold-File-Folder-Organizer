using System;
using System.Collections.Generic;

namespace TimeFold.Core.Files
{
    // The ListViewItem comparer of the WinForms version is gone; the WPF grid sorts through Sort below.
    public static class FileItemComparer
    {
        public static void Sort(List<FileItem> list, int column, bool ascending, bool hasMediaDateColumn = false)
        {
            if (list == null || list.Count <= 1) return;

            static DateTime GetMediaDate(FileItem f) => f.MediaDateTaken ?? DateTime.MinValue;

            Comparison<FileItem> comparison;
            if (hasMediaDateColumn)
            {
                comparison = column switch
                {
                    0 => (x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase),
                    1 => CompareType,
                    2 => (x, y) => DateTime.Compare(x.ModifiedDate, y.ModifiedDate),
                    3 => (x, y) => DateTime.Compare(x.CreatedDate, y.CreatedDate),
                    4 => (x, y) => DateTime.Compare(GetMediaDate(x), GetMediaDate(y)),
                    5 => (x, y) => string.Compare(x.TargetFolder, y.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                    6 => (x, y) => string.Compare(x.TargetFolder, y.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                    7 => CompareSize,
                    _ => (x, y) => DateTime.Compare(x.ModifiedDate, y.ModifiedDate)
                };
            }
            else
            {
                comparison = column switch
                {
                    0 => (x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase),
                    1 => CompareType,
                    2 => (x, y) => DateTime.Compare(x.ModifiedDate, y.ModifiedDate),
                    3 => (x, y) => DateTime.Compare(x.CreatedDate, y.CreatedDate),
                    4 => (x, y) => string.Compare(x.TargetFolder, y.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                    5 => (x, y) => string.Compare(x.TargetFolder, y.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                    6 => CompareSize,
                    _ => (x, y) => DateTime.Compare(x.ModifiedDate, y.ModifiedDate)
                };
            }

            if (ascending) list.Sort(comparison);
            else list.Sort((x, y) => comparison(y, x));
        }

        private static int CompareType(FileItem f1, FileItem f2)
        {
            if (f1.IsDirectory != f2.IsDirectory)
                return f1.IsDirectory ? -1 : 1;
            return string.Compare(f1.TypeDisplay, f2.TypeDisplay, StringComparison.CurrentCultureIgnoreCase);
        }

        private static int CompareSize(FileItem f1, FileItem f2)
        {
            if (f1.IsDirectory && f2.IsDirectory) return string.Compare(f1.Name, f2.Name, StringComparison.CurrentCultureIgnoreCase);
            if (f1.IsDirectory) return -1;
            if (f2.IsDirectory) return 1;
            return f1.Size.CompareTo(f2.Size);
        }
    }
}
