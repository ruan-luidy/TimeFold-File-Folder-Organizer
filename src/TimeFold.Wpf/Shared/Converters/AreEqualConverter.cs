using System.Globalization;
using System.Windows.Data;

namespace TimeFold.Wpf.Shared.Converters
{
    // True when both values are equal (the settings page whose Tag is the current page)
    public sealed class AreEqualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values.Length == 2 && Equals(values[0], values[1]);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
