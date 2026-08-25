using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TelePick.Desktop.Converters
{
    public class EmptyStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return parameter ?? "Empty";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
