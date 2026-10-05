using TimeFold.Core.Files;
using TimeFold.Core.Organizing;
using TimeFold.Core.Settings;

namespace TimeFold.Core.Tests
{
    public sealed class TargetFolderResolverTests
    {
        [Fact]
        public void Subtitle_follows_its_movie()
        {
            var movie = new FileItem { Name = "Film.mkv", FullPath = @"C:\Film.mkv", ModifiedDate = DateTime.Now };
            movie.TargetFolder = TargetFolderResolver.Resolve(movie, OrganizationMode.Category, FolderFormat.YearMonth, "", "");
            var sub = new FileItem { Name = "Film.en.srt", FullPath = @"C:\Film.en.srt", ModifiedDate = DateTime.Now };
            sub.TargetFolder = TargetFolderResolver.Resolve(sub, OrganizationMode.Category, FolderFormat.YearMonth, "", "");

            TargetFolderResolver.ApplySubtitleCompanionPairing([movie, sub]);

            Assert.Equal(movie.TargetFolder, sub.TargetFolder);
        }

        [Fact]
        public void Category_prefix_and_suffix()
        {
            var json = new FileItem { Name = "app.json", FullPath = @"C:\app.json", ModifiedDate = DateTime.Now };

            string folder = TargetFolderResolver.Resolve(json, OrganizationMode.Category, FolderFormat.YearMonth, "", "", "Pre_", "_Post");

            Assert.Equal("Pre_JSON Files_Post", folder);
        }

        [Fact]
        public void Git_repositories_are_routed_per_mode()
        {
            var gitRepo = new FileItem { Name = "my-repo", FullPath = @"C:\Source\my-repo", IsDirectory = true, IsGitRepository = true, ModifiedDate = new DateTime(2026, 9, 24) };
            var normalFolder = new FileItem { Name = "my-docs", FullPath = @"C:\Source\my-docs", IsDirectory = true, IsGitRepository = false };

            Assert.Equal("Git Repos", TargetFolderResolver.Resolve(gitRepo, OrganizationMode.Category, FolderFormat.YearMonth, "", ""));
            Assert.StartsWith("2026", TargetFolderResolver.Resolve(gitRepo, OrganizationMode.Date, FolderFormat.YearMonth, "", ""));
            Assert.Equal("Git Repos", TargetFolderResolver.Resolve(gitRepo, OrganizationMode.Extension, FolderFormat.YearMonth, "", ""));
            Assert.Equal("Grouped Folders", TargetFolderResolver.Resolve(gitRepo, OrganizationMode.Extension, FolderFormat.YearMonth, "", "", "", "", false));
            Assert.Equal("Grouped Folders", TargetFolderResolver.Resolve(normalFolder, OrganizationMode.Extension, FolderFormat.YearMonth, "", ""));
            Assert.StartsWith("Git Repos", TargetFolderResolver.Resolve(gitRepo, OrganizationMode.CategoryAndDate, FolderFormat.YearMonth, "", ""));
            Assert.EndsWith("Git Repos", TargetFolderResolver.Resolve(gitRepo, OrganizationMode.DateAndCategory, FolderFormat.YearMonth, "", ""));
            Assert.Equal("Grouped Folders", TargetFolderResolver.Resolve(gitRepo, OrganizationMode.Category, FolderFormat.YearMonth, "", "", "", "", false));
        }

        [Fact]
        public void Media_date_taken_wins_over_file_dates()
        {
            var photo = new FileItem
            {
                Name = "vacation.jpg",
                ModifiedDate = new DateTime(2026, 9, 20),
                CreatedDate = new DateTime(2026, 9, 26),
                MediaDateTaken = new DateTime(2021, 7, 14),
                IsMediaDateActive = true
            };

            string folder = TargetFolderResolver.Resolve(photo, OrganizationMode.Date, FolderFormat.YearMonth, "", "");

            Assert.StartsWith("2021 July", folder);
        }
    }
}
