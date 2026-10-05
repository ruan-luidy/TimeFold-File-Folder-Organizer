using System.Windows.Media;
using MahApps.Metro.IconPacks;

namespace TimeFold.Wpf.Features.Tour
{
    public sealed record TourSlide(PackIconPhosphorIconsKind Icon, string BadgeText, Color BadgeColor, string Title, string Subtitle, string[] BulletPoints);

    // The slides of WelcomeTourForm, word for word; the emoji badges became Phosphor icons
    public static class TourSlides
    {
        public static TourSlide[] Create()
        {
            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);
            var threeMonthsAgo = now.AddMonths(-3);

            return
            [
                new TourSlide(
                    PackIconPhosphorIconsKind.PaletteBold,
                    "APPEARANCE",
                    Color.FromRgb(99, 102, 241),
                    "Personalize Your Look & Experience",
                    "Select your preferred visual theme. You can always change this in Preferences.",
                    [
                        "Instant Live Preview: Notice how TimeFold instantly updates to match your selection above.",
                        "Distraction-Free Productivity: Choose Light or Dark theme tailored for daytime focus or night comfort.",
                        "100% Non-Destructive Guarantee: TimeFold never deletes, overwrites, or alters your personal files.",
                        "Ready to explore? Click 'Next' to see how TimeFold solves messy, freezing folders!"
                    ]),
                new TourSlide(
                    PackIconPhosphorIconsKind.WarningBold,
                    "THE PROBLEM",
                    Color.FromRgb(239, 68, 68),
                    "Is Your Folder Slow, Cluttered, and Freezing?",
                    "Thousands of unorganized files in one place ruin productivity.",
                    [
                        "Sluggish Windows Explorer: Folders like Downloads, Screenshots, Photos, or Desktop take forever to load and can freeze your PC.",
                        "Lost in the Chaos: Scrolling through hundreds or thousands of loose files to locate one specific document wastes time.",
                        "Zero Structure: Years of mixed downloads, media, and receipts pile up into an unmanageable mountain of clutter.",
                        "The Solution: TimeFold was built to solve this exact headache cleanly in just one single click."
                    ]),
                new TourSlide(
                    PackIconPhosphorIconsKind.LightningBold,
                    "THE 1-CLICK SOLUTION",
                    Color.FromRgb(14, 165, 233),
                    "Instant Order: Smart Date-Wise Organization",
                    "Intelligently examines true file timestamps and groups items into dedicated folders.",
                    [
                        $"Dedicated Monthly Folders: Files from last month automatically move into their own folder (e.g., {lastMonth:yyyy-MM} or {lastMonth:yyyy MMM}).",
                        $"Older Archives: Files from three months ago move cleanly into {threeMonthsAgo:yyyy-MM} ({threeMonthsAgo:yyyy MMM}).",
                        $"Day-by-Day Organization: Want daily sorting? Choose day-wise folders (e.g., {now:yyyy-MM-dd}) — ideal for daily screenshots or camera photos.",
                        "Smart Date Defaults: Files use Date Modified, folders use Date Created, and photos/videos prioritize camera EXIF Date Taken. You can easily change this anytime in Preferences.",
                        $"Custom Naming: Add custom prefixes and suffixes (e.g., Photos_{lastMonth:yyyy-MM}) to match your exact naming preferences."
                    ]),
                new TourSlide(
                    PackIconPhosphorIconsKind.SquaresFourBold,
                    "SMART CATEGORIES",
                    Color.FromRgb(245, 158, 11),
                    "Organize by File Type, Date, or Both",
                    "Tailor your organization strategy with categories and 2-level hybrid nesting.",
                    [
                        "Smart Categories: Automatically group files into intuitive categories (Images, Documents, Audio, Video, 3D, Code, Archives, etc.).",
                        $"Hybrid 2-Level Folders: Combine both dimensions (e.g., Images\\{now:yyyy-MM} or {now:yyyy-MM}\\Images) for deep organization.",
                        "Custom Type Rules: Remap file extensions, add custom folder prefixes & suffixes, or create your own custom categories.",
                        "Smart Companion Pairing: Automatically packages matching video & subtitle pairs into dedicated movie folders, and keeps HTML web pages with their asset folders.",
                        "1-Click Switching: Seamlessly toggle between Date, Category, and Hybrid modes directly from the main toolbar."
                    ]),
                new TourSlide(
                    PackIconPhosphorIconsKind.ShieldCheckBold,
                    "SAFE & REVERSIBLE",
                    Color.FromRgb(16, 185, 129),
                    "Preview First, Move with Confidence",
                    "Nothing is moved blindly. You stay in 100% control at every step.",
                    [
                        "100% Non-Destructive Guarantee: TimeFold only reorganizes your files into neat folders — it never deletes, alters, or compresses your original files.",
                        "Interactive Preview & Checkboxes: See the exact target destination for every file before clicking Start. Check or uncheck individual items on the fly for that session.",
                        "Folder Exclusion Rules & .timefold-ignore: Protect sensitive folders by rule or drop a .timefold-ignore file into any folder to permanently lock and skip it.",
                        "Automatic CSV Audit Log: Every organization creates a detailed timestamped CSV log so you always know where files went.",
                        "In-Place or Custom Output: Organize directly inside the source folder or route sorted files to an external backup drive."
                    ]),
                new TourSlide(
                    PackIconPhosphorIconsKind.RocketLaunchBold,
                    "SCALE & SPEED",
                    Color.FromRgb(168, 85, 247),
                    "Heavy-Duty Speed for 100,000+ Items",
                    "Engineered to effortlessly handle massive personal and professional libraries.",
                    [
                        "Blazing Fast Engine: Seamlessly browse 10,000 to 100,000+ items with smooth paginated loading without freezing your PC.",
                        "Smart Timestamp Detection: Automatically warns you if files share identical timestamps (common with downloaded ZIPs or chat media).",
                        "Include Folders Option: Optionally organize entire loose folders alongside files with a single checkbox.",
                        "Drag & Drop Ready: Simply drag and drop any folder into TimeFold to start organizing immediately!"
                    ]),
            ];
        }
    }
}
