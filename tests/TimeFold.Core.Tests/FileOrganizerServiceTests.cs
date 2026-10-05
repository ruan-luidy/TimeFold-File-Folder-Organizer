using TimeFold.Core.Config;
using TimeFold.Core.Conflicts;
using TimeFold.Core.Files;
using TimeFold.Core.Organizing;
using TimeFold.Core.Settings;

namespace TimeFold.Core.Tests
{
    public sealed class FileOrganizerServiceTests
    {
        [Fact]
        public void Git_detection_looks_one_wrapper_level_deep()
        {
            using var root = new TempDir("GitTest");
            string repoL1 = root.Combine("RepoL1");
            Directory.CreateDirectory(Path.Combine(repoL1, ".github"));
            string wrapperL2 = root.Combine("WrapperL2");
            Directory.CreateDirectory(Path.Combine(wrapperL2, "InnerRepo", ".github"));
            string workspace = root.Combine("Workspace");
            Directory.CreateDirectory(Path.Combine(workspace, "ProjectA", ".github"));
            Directory.CreateDirectory(Path.Combine(workspace, "ProjectB"));

            Assert.True(FileOrganizerService.IsGitRepository(repoL1));
            Assert.True(FileOrganizerService.IsGitRepository(wrapperL2));
            Assert.False(FileOrganizerService.IsGitRepository(workspace));
        }

        [Fact]
        public async Task Folder_moves_into_its_group_in_place()
        {
            using var dir = new TempDir("OrgTest");
            string testSubdir = dir.Combine("TestFolder");
            Directory.CreateDirectory(testSubdir);
            File.WriteAllText(Path.Combine(testSubdir, "data.txt"), "test");
            var item = new FileItem { Name = "TestFolder", FullPath = testSubdir, IsDirectory = true, TargetFolder = "Grouped Folders" };

            var organizer = new FileOrganizerService(dir.Combine("TimeFold.exe"), dir.Path, dir.Path);
            organizer.ApplyNamingSettings(FolderFormat.YearMonth, "", "", false, OrganizationMode.Extension, createSortedSubfolder: false);
            var result = await organizer.OrganizeFilesAsync([item], null!, CancellationToken.None, false, ConflictResolutionStrategy.AutoRename);

            Assert.Equal(1, result.FilesMoved);
            Assert.True(Directory.Exists(dir.Combine("Grouped Folders", "TestFolder")));
        }

        [Fact]
        public async Task Cancellation_keeps_last_result_for_undo()
        {
            using var dir = new TempDir("CancelTest");
            string file = dir.Combine("test1.txt");
            File.WriteAllText(file, "hello");
            var item = new FileItem { Name = "test1.txt", FullPath = file, TargetFolder = "TXT" };
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var organizer = new FileOrganizerService(dir.Combine("TimeFold.exe"), dir.Path, dir.Path);
            try
            {
                await organizer.OrganizeFilesAsync([item], null!, cts.Token, false);
            }
            catch (OperationCanceledException) { }

            Assert.NotNull(organizer.LastResult);
        }

        [Fact]
        public async Task Exclusions_and_unchecked_items_stay_at_source()
        {
            using var dir = new TempDir("ExclTest");
            string myFiles = dir.Combine("MYFILES");
            Directory.CreateDirectory(myFiles);
            File.WriteAllText(Path.Combine(myFiles, "secret.txt"), "data");
            Directory.CreateDirectory(dir.Combine("Projects"));
            File.WriteAllText(dir.Combine("doc.pdf"), "pdf content");
            string notes = dir.Combine("notes.txt");
            File.WriteAllText(notes, "notes content");

            var organizer = new FileOrganizerService(dir.Combine("TimeFold.exe"), dir.Path, dir.Path);
            organizer.ApplyNamingSettings(FolderFormat.YearMonth, "", "", false, OrganizationMode.Category, createSortedSubfolder: false);

            // the rule matches despite the spaces and the case
            var rules = new List<string> { "  myfiles  " };
            var scanned = organizer.ScanFiles(includeTopLevelFolders: true, excludedFolders: rules, enableFolderExclusions: true);
            var excluded = scanned.Single(f => f.Name.Equals("MYFILES", StringComparison.OrdinalIgnoreCase));
            Assert.True(excluded.IsExcludedByRule);
            Assert.False(excluded.IsSelected);
            Assert.Equal("— (Ignored)", excluded.TargetFolder);

            scanned.Single(f => f.Name == "notes.txt").IsSelected = false;
            await organizer.OrganizeFilesAsync(scanned, null!, CancellationToken.None, false);

            Assert.True(File.Exists(Path.Combine(myFiles, "secret.txt")));
            Assert.True(File.Exists(notes));

            // with exclusions paused the folder is organized like any other
            var paused = organizer.ScanFiles(includeTopLevelFolders: true, excludedFolders: rules, enableFolderExclusions: false);
            var toggled = paused.Single(f => f.Name.Equals("MYFILES", StringComparison.OrdinalIgnoreCase));
            Assert.False(toggled.IsExcludedByRule);
            Assert.True(toggled.IsSelected);
        }

        [Fact]
        public void Ignore_marker_excludes_the_folder_and_is_never_listed()
        {
            using var dir = new TempDir("MarkerTest");
            string projects = dir.Combine("Projects");
            Directory.CreateDirectory(projects);
            File.WriteAllBytes(Path.Combine(projects, AppConstants.TimefoldIgnoreFileName), []);
            File.WriteAllText(dir.Combine(AppConstants.TimefoldIgnoreFileName), "");

            var organizer = new FileOrganizerService(dir.Combine("TimeFold.exe"), dir.Path, dir.Path);
            organizer.ApplyNamingSettings(FolderFormat.YearMonth, "", "", false, OrganizationMode.Category, createSortedSubfolder: false);
            var scanned = organizer.ScanFiles(includeTopLevelFolders: true, enableFolderExclusions: true);

            Assert.DoesNotContain(scanned, f => f.Name.Equals(AppConstants.TimefoldIgnoreFileName, StringComparison.OrdinalIgnoreCase));
            var project = scanned.Single(f => f.Name == "Projects");
            Assert.True(project.IsExcludedByRule);
            Assert.Equal("📁 Folder (Ignored)", project.TypeDisplay);
            Assert.Equal("— (Ignored)", project.TargetFolder);
            Assert.True(scanned.OrderByDescending(f => f.IsExcludedByRule).First().IsExcludedByRule);
        }
    }
}
