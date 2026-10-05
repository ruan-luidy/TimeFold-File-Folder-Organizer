using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TimeFold.Wpf.Features.FileTypes
{
    public partial class FileTypesPage : UserControl
    {
        public FileTypesPage()
        {
            InitializeComponent();
            DataContextChanged += (_, e) =>
            {
                if (e.OldValue is FileTypesViewModel old) old.RemapRequested -= OnRemapRequested;
                if (e.NewValue is FileTypesViewModel vm) vm.RemapRequested += OnRemapRequested;
            };
        }

        private void OnRemapRequested(object? sender, EventArgs e)
        {
            CategoryField.Focus();
            CategoryField.SelectAll();
        }

        private void Rules_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var current = e.OriginalSource as DependencyObject;
            while (current != null && current is not DataGridRow)
                current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            if (current is DataGridRow row) row.IsSelected = true;
        }
    }
}
