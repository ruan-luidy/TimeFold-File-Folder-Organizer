using TimeFold.Core.Files;
using TimeFold.Core.Undo;

namespace TimeFold.Core.Tests
{
    public sealed class UndoServiceTests : IDisposable
    {
        public void Dispose() => UndoService.ClearSession();

        [Fact]
        public async Task Undo_restores_prunes_and_removes_the_log()
        {
            using var dir = new TempDir("UndoSanity");
            string srcDir = dir.Combine("Source");
            string outDir = dir.Combine("Output");
            string sortedDir = Path.Combine(outDir, "Sorted_Test");
            string monthDir = Path.Combine(sortedDir, "2026 January");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(monthDir);

            string origPath = Path.Combine(srcDir, "doc.txt");
            string destPath = Path.Combine(monthDir, "doc.txt");
            File.WriteAllText(destPath, "TimeFold Undo Test Content");
            var item = new FileItem { Name = "doc.txt", FullPath = origPath, DestinationPath = destPath, ModifiedDate = DateTime.Now };
            string dummyLog = Path.Combine(outDir, "TimeFold_Log_Test.csv");
            File.WriteAllText(dummyLog, "timestamp,orig,dest");

            var session = UndoService.RecordSession(srcDir, outDir, sortedDir, [item], [monthDir, sortedDir], dummyLog);
            Assert.Equal(1, UndoService.PreflightCheck(session).ReadyToRestore);

            var result = await UndoService.UndoAsync(session, null, CancellationToken.None);

            Assert.Equal(1, result.RestoredCount);
            Assert.True(File.Exists(origPath));
            Assert.False(Directory.Exists(monthDir));
            Assert.False(File.Exists(dummyLog));
        }

        [Fact]
        public void Sessions_expire_after_seven_days()
        {
            Assert.True(UndoService.IsSessionExpired(new UndoSession { Timestamp = DateTime.Now.AddDays(-8) }));
            Assert.False(UndoService.IsSessionExpired(new UndoSession { Timestamp = DateTime.Now.AddDays(-2) }));
        }

        [Fact]
        public void Items_that_did_not_move_are_not_recorded()
        {
            var untouched = new FileItem { FullPath = @"C:\Source\Untouched", DestinationPath = @"C:\Source\Untouched", IsDirectory = true };
            var moved = new FileItem { FullPath = @"C:\Source\Moved", DestinationPath = @"C:\Output\Moved", IsDirectory = true };

            var session = UndoService.RecordSession(@"C:\Source", @"C:\Output", "", [untouched, moved]);

            Assert.Single(session.MovedItems);
            Assert.Equal(@"C:\Source\Moved", session.MovedItems[0].OriginalPath);
        }
    }
}
