using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using TimeFold.Core.Config;

namespace TimeFold.Wpf.Features.Explorer
{
    /// <summary>
    /// The "Open in TimeFold" context menu written by the installer, and where the config lives
    /// (IsInstalledBuild, IsContextMenuRegistered and OpenConfigLocation of the old AppConstants).
    /// </summary>
    public static class ExplorerIntegration
    {
        // True only when running from the installed directory (Inno Setup writes the HKLM marker)
        private static readonly Lazy<bool> _lazyIsInstalled = new(() =>
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"Software\{AppConstants.VendorName}\{AppConstants.ShortAppName}");
                if (key == null) return false;

                string currentDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                string? installPath = key.GetValue("InstallPath") as string;
                if (!string.IsNullOrWhiteSpace(installPath) &&
                    string.Equals(currentDir, installPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Fallback: running inside Program Files
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).TrimEnd('\\', '/');
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).TrimEnd('\\', '/');
                return currentDir.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ||
                       currentDir.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });

        public static bool IsInstalledBuild => _lazyIsInstalled.Value;

        public static bool IsContextMenuRegistered
        {
            get
            {
                try
                {
                    using var key1 = Registry.ClassesRoot.OpenSubKey(@"Directory\shell\TimeFold");
                    using var key2 = Registry.ClassesRoot.OpenSubKey(@"Directory\Background\shell\TimeFold");
                    return key1 != null || key2 != null;
                }
                catch { return false; }
            }
        }

        public static void OpenConfigLocation()
        {
            string dir = AppConstants.GetConfigDirectoryPath();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string file = AppConstants.GetConfigFilePath();
            if (File.Exists(file))
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{file}\"", UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
    }
}
