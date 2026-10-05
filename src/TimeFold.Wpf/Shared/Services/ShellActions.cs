using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TimeFold.Wpf.Shared.Services
{
    /// <summary>Opening things in Windows: files, folders, Explorer selection, links and the Recycle Bin.</summary>
    public static class ShellActions
    {
        public static void Open(string path) =>
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

        public static void OpenFolder(string folder) => Process.Start("explorer.exe", $"\"{folder}\"");

        public static void Reveal(string path)
        {
            if (File.Exists(path)) Process.Start("explorer.exe", $"/select,\"{path}\"");
            else if (Directory.Exists(path)) OpenFolder(path);
        }

        public static void OpenUrl(string url)
        {
            try { Open(url); } catch { }
        }

        /// <summary>
        /// Same as VisualBasic's DeleteFile/DeleteDirectory with SendToRecycleBin in the WinForms build;
        /// that API only ships with Windows Forms, so this calls the shell directly.
        /// </summary>
        public static void SendToRecycleBin(string path)
        {
            var op = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = path + '\0' + '\0',
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
            };
            int result = SHFileOperation(ref op);
            if (result != 0 || op.fAnyOperationsAborted)
                throw new IOException($"Windows could not move the item to the Recycle Bin (code {result}).");
        }

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_SILENT = 0x0004;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOERRORUI = 0x0400;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string? pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string? lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);
    }
}
