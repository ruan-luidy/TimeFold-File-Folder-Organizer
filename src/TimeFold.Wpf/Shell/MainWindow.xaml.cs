using System.Windows;
using System.Windows.Controls.Primitives;

namespace TimeFold.Wpf.Shell
{
    public partial class MainWindow : HandyControl.Controls.Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = HelpButton.ContextMenu;
            menu.DataContext = DataContext;
            menu.PlacementTarget = HelpButton;
            menu.Placement = PlacementMode.Bottom;
            menu.VerticalOffset = 4;
            menu.IsOpen = true;
        }
    }
}
