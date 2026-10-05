using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace TimeFold.Wpf.Features.Source
{
    public partial class SourceView : UserControl
    {
        public SourceView()
        {
            InitializeComponent();
        }

        private SourceViewModel ViewModel => (SourceViewModel)DataContext;

        // the recent folders menu is built when opened, so it is always current
        private void RecentButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = RecentButton, Placement = PlacementMode.Bottom, VerticalOffset = 4 };

            if (!ViewModel.HasRecent)
            {
                menu.Items.Add(new MenuItem { Header = "(No recent folders)", IsEnabled = false });
            }
            else
            {
                int i = 1;
                foreach (var recent in ViewModel.RecentFolders)
                {
                    menu.Items.Add(new MenuItem
                    {
                        Header = $"_{i++}. {recent.Name}",
                        InputGestureText = recent.Path,
                        ToolTip = recent.Path,
                        Command = ViewModel.OpenRecentCommand,
                        CommandParameter = recent,
                    });
                }
                menu.Items.Add(new Separator());
                menu.Items.Add(new MenuItem { Header = "Clear Recent History", Command = ViewModel.ClearRecentCommand });
            }

            menu.IsOpen = true;
        }
    }
}
