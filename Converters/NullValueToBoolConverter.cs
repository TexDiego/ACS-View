using ACS_View.Domain.ValueObjects;
using System.Globalization;

namespace ACS_View.Converters
{
    internal class NullValueToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s) return !string.IsNullOrEmpty(s);

            if (value is List<HealthIcon> h) return h.Count > 0;

            if (value is HealthIcon i) return i == null;

            return value is not null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}