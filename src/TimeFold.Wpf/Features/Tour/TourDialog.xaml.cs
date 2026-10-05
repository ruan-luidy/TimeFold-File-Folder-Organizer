using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TimeFold.Wpf.Shared.Controls;
using TimeFold.Wpf.Shared.Icons;

namespace TimeFold.Wpf.Features.Tour
{
    public readonly record struct TourResult(bool DarkMode, bool DontShowOnStartup);

    public sealed record TourBullet(string Head, string Body);

    // The welcome tour (WelcomeTourForm): six slides, the first one picks the theme and applies it live
    public partial class TourDialog : DialogCard
    {
        private readonly TourSlide[] _slides = TourSlides.Create();
        private readonly Action<bool> _onThemeChanged;
        private int _index;
        private bool _isDark;

        public TourDialog(bool isDark, bool dontShowOnStartup, Action<bool> onThemeChanged)
        {
            InitializeComponent();
            _isDark = isDark;
            _onThemeChanged = onThemeChanged;
            DontShowCheck.IsChecked = dontShowOnStartup;
            (isDark ? DarkOption : LightOption).IsChecked = true;
            CancelResult = null;
            Show(0);
        }

        private TourResult CurrentResult => new(_isDark, DontShowCheck.IsChecked == true);

        private void Show(int index)
        {
            if (index < 0 || index >= _slides.Length) return;
            _index = index;
            var slide = _slides[index];

            StepText.Text = $"Step {index + 1} of {_slides.Length}";
            var color = new SolidColorBrush(slide.BadgeColor);
            Badge.Background = new SolidColorBrush(Color.FromArgb(0x26, slide.BadgeColor.R, slide.BadgeColor.G, slide.BadgeColor.B));
            BadgeIcon.Data = PhosphorExtension.Get(slide.Icon);
            BadgeIcon.Fill = color;
            BadgeText.Text = slide.BadgeText;
            BadgeText.Foreground = color;
            SlideTitle.Text = slide.Title;
            SlideSubtitle.Text = slide.Subtitle;
            ThemeChoice.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;

            // "Head: body" bullets show the head in bold
            Bullets.ItemsSource = slide.BulletPoints.Select(b =>
            {
                int colon = b.IndexOf(": ", StringComparison.Ordinal);
                return colon > 0 ? new TourBullet(b[..(colon + 1)] + " ", b[(colon + 2)..]) : new TourBullet(string.Empty, b);
            }).ToList();

            Dots.ItemsSource = Enumerable.Range(0, _slides.Length).Select(i => i == index).ToList();
            BackButton.Visibility = index > 0 ? Visibility.Visible : Visibility.Collapsed;
            NextButton.Content = index == _slides.Length - 1 ? "Start Organizing" : "Next";
        }

        private void Theme_Checked(object sender, RoutedEventArgs e)
        {
            bool dark = DarkOption.IsChecked == true;
            if (dark == _isDark) return;
            _isDark = dark;
            _onThemeChanged(dark);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_index < _slides.Length - 1) Show(_index + 1);
            else Finish(CurrentResult);
        }

        private void Back_Click(object sender, RoutedEventArgs e) => Show(_index - 1);

        private void Skip_Click(object sender, RoutedEventArgs e) => Finish(CurrentResult);

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // Esc keeps the theme picked so far, like closing the old tour window
            if (e.Key == Key.Escape)
            {
                Finish(CurrentResult);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Right) { Next_Click(this, e); e.Handled = true; }
            else if (e.Key == Key.Left) { Back_Click(this, e); e.Handled = true; }
            base.OnPreviewKeyDown(e);
        }
    }
}
