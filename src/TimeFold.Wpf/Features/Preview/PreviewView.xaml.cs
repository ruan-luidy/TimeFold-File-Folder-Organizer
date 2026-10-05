using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TimeFold.Wpf.Features.Preview
{
    public partial class PreviewView : UserControl
    {
        private PreviewViewModel? _viewModel;

        public PreviewView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelChanged;
                _viewModel.SortChanged -= OnSortChanged;
            }

            _viewModel = DataContext as PreviewViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelChanged;
                _viewModel.SortChanged += OnSortChanged;
                ShowTakenColumn();
                OnSortChanged(this, EventArgs.Empty);
            }
        }

        // a DataGridColumn is not in the visual tree, so its visibility is set from here
        private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PreviewViewModel.HasMediaDateColumn))
                ShowTakenColumn();
        }

        private void ShowTakenColumn() =>
            TakenColumn.Visibility = _viewModel?.HasMediaDateColumn == true ? Visibility.Visible : Visibility.Collapsed;

        // The model list is sorted by FileItemComparer (the grid only shows a page of it), so the grid never sorts itself
        private void Grid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            if (!string.IsNullOrEmpty(e.Column.SortMemberPath))
                _viewModel?.SortBy(e.Column.SortMemberPath);
        }

        private void OnSortChanged(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;
            var (column, ascending) = _viewModel.Sort;
            string key = _viewModel.SortKey(column);
            foreach (var c in Grid.Columns)
            {
                c.SortDirection = c.SortMemberPath == key
                    ? (ascending ? ListSortDirection.Ascending : ListSortDirection.Descending)
                    : null;
            }
        }

        // right click selects the row under the mouse, so the menu acts on it
        private void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) is { } row)
                row.IsSelected = true;
        }

        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FindAncestor<CheckBox>(e.OriginalSource as DependencyObject) != null) return;
            if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) != null)
                _viewModel?.OpenSelectedCommand.Execute(null);
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel?.SelectedRow is not { } row) return;
            switch (e.Key)
            {
                case Key.Space:
                    row.IsChecked = !row.IsChecked;
                    e.Handled = true;
                    break;
                case Key.Enter:
                    _viewModel.OpenSelectedCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F2 when !row.Item.IsDirectory:
                    _viewModel.RenameSelectedCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Delete:
                    _viewModel.DeleteSelectedCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }

        private static T? FindAncestor<T>(DependencyObject? current)
            where T : DependencyObject
        {
            while (current != null && current is not T)
                current = current is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            return current as T;
        }
    }
}
