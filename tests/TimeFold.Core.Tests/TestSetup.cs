using System.Runtime.CompilerServices;

// FileTypeService and UndoService keep static state, so the tests run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace TimeFold.Core.Tests
{
    internal static class TestSetup
    {
        // Runs before any test touches AppConstants: settings, custom types and undo go to a temp folder.
        [ModuleInitializer]
        internal static void UseTempConfig()
        {
            string dir = Path.Combine(Path.GetTempPath(), "TimeFold_TestConfig_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            Environment.SetEnvironmentVariable("TIMEFOLD_CONFIG_DIR", dir);
        }
    }

    internal sealed class TempDir : IDisposable
    {
        public TempDir(string name)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TimeFold_{name}_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, true); } catch { }
        }
    }
}
