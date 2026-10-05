using System.Buffers.Binary;
using TimeFold.Core.Config;
using TimeFold.Core.Exclusions;
using TimeFold.Core.Files;
using TimeFold.Core.Naming;
using TimeFold.Core.Settings;

namespace TimeFold.Core.Tests
{
    public sealed class FormattingTests
    {
        [Fact]
        public void Sort_on_the_media_date_column()
        {
            var a = new FileItem { Name = "a.jpg", MediaDateTaken = new DateTime(2020, 1, 1), IsMediaDateActive = true };
            var b = new FileItem { Name = "b.jpg", MediaDateTaken = new DateTime(2022, 1, 1), IsMediaDateActive = true };
            var list = new List<FileItem> { b, a };

            FileItemComparer.Sort(list, 4, true, hasMediaDateColumn: true);

            Assert.Equal("a.jpg", list[0].Name);
        }

        [Fact]
        public void Tooltip_date_format()
        {
            Assert.Equal("15 Aug 2020 03:30:22 PM", AppConstants.FormatTooltipDate(new DateTime(2020, 8, 15, 15, 30, 22)));
        }

        [Fact]
        public void Media_tooltip_explains_how_to_turn_it_off()
        {
            var photo = new FileItem
            {
                Name = "photo.jpg",
                MediaDateTaken = new DateTime(2023, 8, 14, 15, 30, 22),
                ModifiedDate = new DateTime(2026, 9, 20, 11, 45, 10),
                CreatedDate = new DateTime(2026, 9, 26, 18, 20, 0),
                IsMediaDateActive = true,
                TargetFolder = "2023-08"
            };

            string tip = AppConstants.BuildDateTooltip(photo);

            Assert.Contains("📷 Date Taken:", tip);
            Assert.Contains("disable 'Prioritize original media Date Taken", tip);
        }

        [Fact]
        public void Mp4_creation_date_is_read_from_mvhd()
        {
            using var dir = new TempDir("Mp4Test");
            string mp4 = dir.Combine("test_clip.mp4");
            // mid-year, so ToLocalTime stays in 2024 in any timezone (Jan 1 UTC is still 2023 in Brazil)
            ulong sec2024 = (ulong)(new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc) - new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            using (var fs = new FileStream(mp4, FileMode.Create))
            {
                fs.Write([0, 0, 0, 16, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6f, 0x6d, 0, 0, 0, 0]);
                fs.Write([0, 0, 0, 36, 0x6d, 0x6f, 0x6f, 0x76]);
                fs.Write([0, 0, 0, 28, 0x6d, 0x76, 0x68, 0x64, 0, 0, 0, 0]);
                byte[] secBuf = new byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(secBuf, (uint)sec2024);
                fs.Write(secBuf);
                fs.Write(new byte[12]);
            }

            var date = MediaDateExtractor.TryGetDateTaken(mp4);

            Assert.Equal(2024, date?.Year);
        }

        [Fact]
        public void Every_folder_format_survives_split_and_resolve()
        {
            foreach (var format in Enum.GetValues<FolderFormat>())
            {
                var (core, flipped, shortMonth) = DateFormatChoice.Split(format);
                Assert.Equal(format, DateFormatChoice.Resolve(core, flipped, shortMonth));
            }
        }

        [Fact]
        public void Ignore_markers_follow_the_list()
        {
            using var dir = new TempDir("MarkerApply");
            Directory.CreateDirectory(dir.Combine("Keep"));
            Directory.CreateDirectory(dir.Combine("Skip"));
            IgnoreMarker.Add(dir.Combine("Keep"));

            IgnoreMarker.Apply(dir.Path, ["skip "]);

            Assert.Equal(["Skip"], IgnoreMarker.ListIgnored(dir.Path));
        }
    }
}
