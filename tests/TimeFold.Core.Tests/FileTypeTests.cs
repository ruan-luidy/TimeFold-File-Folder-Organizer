using TimeFold.Core.Files;
using TimeFold.Core.FileTypes;
using TimeFold.Core.Organizing;
using TimeFold.Core.Settings;

namespace TimeFold.Core.Tests
{
    public sealed class FileTypeTests
    {
        [Theory]
        [InlineData(".json", "JSON Files")]
        [InlineData(".jsonc", "JSON Files")]
        [InlineData(".xml", "Data & Config Files")]
        [InlineData(".yaml", "Data & Config Files")]
        [InlineData(".env", "Data & Config Files")]
        [InlineData(".c3p", "Game Dev Files")]
        [InlineData(".godot", "Game Dev Files")]
        [InlineData(".unitypackage", "Game Dev Files")]
        [InlineData(".yyp", "Game Dev Files")]
        [InlineData(".blend", "3D Files")]
        [InlineData(".3mf", "3D Files")]
        [InlineData(".gltf", "3D Files")]
        [InlineData(".usdz", "3D Files")]
        [InlineData(".flatpak", "App Installers")]
        [InlineData(".appimage", "App Installers")]
        [InlineData(".deb", "App Installers")]
        [InlineData(".dmg", "App Installers")]
        [InlineData(".ipa", "App Installers")]
        [InlineData(".ipk", "App Installers")]
        [InlineData(".apk", "App Installers")]
        [InlineData(".exe", "App Installers")]
        [InlineData(".psd", "Photoshop Files")]
        [InlineData(".psb", "Photoshop Files")]
        [InlineData(".indd", "Publishing Files")]
        [InlineData(".svg", "Vector Files")]
        [InlineData(".ai", "Vector Files")]
        [InlineData(".cs", "Code Files")]
        [InlineData(".mp4", "Video Files")]
        [InlineData(".mp3", "Audio Files")]
        [InlineData(".zip", "Zip & Archives")]
        [InlineData(".ttf", "Font Files")]
        [InlineData(".docx", "Office Files")]
        [InlineData(".pdf", "PDF Files")]
        [InlineData(".epub", "Reader Files")]
        [InlineData(".txt", "Text & Notes")]
        [InlineData(".ps1", "Script Files")]
        [InlineData(".bat", "Script Files")]
        [InlineData(".sh", "Script Files")]
        [InlineData(".lnk", "Shortcuts")]
        [InlineData(".url", "Web Links")]
        [InlineData(".torrent", "Torrent Files")]
        public void Factory_categories_map_known_extensions(string extension, string category)
        {
            Assert.Equal(category, FileTypeService.Instance.GetCategory(extension));
        }

        [Fact]
        public void Extension_override_changes_category_but_not_extension_mode()
        {
            var service = FileTypeService.Instance;
            var pptx = new FileItem { Name = "slides.pptx", FullPath = @"C:\slides.pptx", ModifiedDate = DateTime.Now };

            service.RemoveCategoryOverride(".pptx");
            string defExt = TargetFolderResolver.Resolve(pptx, OrganizationMode.Extension, FolderFormat.YearMonth, "", "");
            service.SetCategoryOverride(".pptx", "CAT");
            string customCat = TargetFolderResolver.Resolve(pptx, OrganizationMode.Category, FolderFormat.YearMonth, "", "");
            string customExt = TargetFolderResolver.Resolve(pptx, OrganizationMode.Extension, FolderFormat.YearMonth, "", "");
            bool isCustom = service.IsCustomRoute(".pptx");
            service.RemoveCategoryOverride(".pptx");
            string revExt = TargetFolderResolver.Resolve(pptx, OrganizationMode.Extension, FolderFormat.YearMonth, "", "");

            Assert.Equal("PPTX", defExt);
            Assert.Equal("CAT", customCat);
            Assert.Equal("PPTX", customExt);
            Assert.True(isCustom);
            Assert.Equal("PPTX", revExt);
        }

        [Fact]
        public void Category_rename_and_reset()
        {
            var service = FileTypeService.Instance;

            service.RenameCategory("PDF Files", "My PDFs");
            Assert.Equal("My PDFs", service.GetCategory(".pdf"));
            Assert.True(service.IsCategoryRenamed("My PDFs", out var original));
            Assert.Equal("PDF Files", original);

            Assert.True(service.ResetCategoryName("My PDFs"));
            Assert.Equal("PDF Files", service.GetCategory(".pdf"));
        }
    }
}
