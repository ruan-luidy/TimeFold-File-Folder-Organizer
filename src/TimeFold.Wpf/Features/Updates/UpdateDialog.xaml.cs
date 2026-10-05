using System.Windows;
using TimeFold.Core.Config;
using TimeFold.Core.Updates;
using TimeFold.Wpf.Shared.Controls;
using TimeFold.Wpf.Shared.Services;

namespace TimeFold.Wpf.Features.Updates
{
    public enum UpdateAction { Download, Skip, Later }

    public partial class UpdateDialog : DialogCard
    {
        private readonly UpdateService.UpdateInfo _info;

        public UpdateDialog(UpdateService.UpdateInfo info)
        {
            InitializeComponent();
            CancelResult = UpdateAction.Later;
            _info = info;

            TitleText.Text = $"TimeFold {info.Version} is available";
            CurrentText.Text = $"You have {AppConstants.AppVersion}";
            SummaryText.Text = string.IsNullOrWhiteSpace(info.Summary) ? "A new version of TimeFold is ready to download." : info.Summary;
            NotesLink.Visibility = info.HasFullNotes ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Notes_Click(object sender, RoutedEventArgs e) => ShellActions.OpenUrl(_info.ReleasePageUrl);

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            ShellActions.OpenUrl(_info.ReleasePageUrl);
            Finish(UpdateAction.Download);
        }

        private void Skip_Click(object sender, RoutedEventArgs e) => Finish(UpdateAction.Skip);

        private void Later_Click(object sender, RoutedEventArgs e) => Finish(UpdateAction.Later);
    }
}
