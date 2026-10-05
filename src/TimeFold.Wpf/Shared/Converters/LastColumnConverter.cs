using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TimeFold.Wpf.Shared.Converters
{
    // "Is this the last visible column?" - its cell and header lose the right separator (it doubled the card edge)
    public sealed class LastColumnConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] is not DataGridColumn column)
                return true;

            if (values[2] is not DataGrid grid)
                return false;

            var last = grid.Columns.Where(c => c.Visibility == Visibility.Visible).MaxBy(c => c.DisplayIndex);
            return ReferenceEquals(column, last);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
