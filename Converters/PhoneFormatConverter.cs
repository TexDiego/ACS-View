using ACS_View.Domain.ValueObjects;
using System.Globalization;

namespace ACS_View.Converters
{
    internal class PhoneFormatConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Phone.Format(value as string);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value as string ?? string.Empty;
        }
    }
}
