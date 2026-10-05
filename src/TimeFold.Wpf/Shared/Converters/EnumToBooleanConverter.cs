using System.Globalization;
using System.Windows.Data;

namespace TimeFold.Wpf.Shared.Converters
{
    // RadioButtons bound to an enum: each button gives its value in ConverterParameter
    public sealed class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is not null && value.Equals(parameter);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? parameter : Binding.DoNothing;
    }
}
