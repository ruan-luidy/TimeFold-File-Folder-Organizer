using System.Windows;
using System.Windows.Controls;

namespace TimeFold.Wpf.Shell
{
    // A folder (or a file, for its folder) dropped anywhere on the page becomes the source, as on the old drop zone
    public partial class OrganizePage : UserControl
    {
        public OrganizePage()
        {
            InitializeComponent();
        }

        private MainViewModel ViewModel => (MainViewModel)DataContext;

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            bool isValid = !ViewModel.Progress.IsActive
                && e.Data.GetDataPresent(DataFormats.FileDrop)
                && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: >= 1 };
            e.Effects = isValid ? DragDropEffects.Copy : DragDropEffects.None;
            ViewModel.Source.IsDragOver = isValid;
            e.Handled = true;
        }

        private void Page_DragLeave(object sender, DragEventArgs e) => ViewModel.Source.IsDragOver = false;

        private void Page_Drop(object sender, DragEventArgs e)
        {
            ViewModel.Source.IsDragOver = false;
            if (ViewModel.Progress.IsActive) return;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files) return;

            if (ViewModel.Organize.IsComplete) ViewModel.Organize.StartNew();
            ViewModel.Source.Drop(files[0]);
        }
    }
}
