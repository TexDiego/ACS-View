using ACS_View.Domain.ValueObjects;
using System.Globalization;

namespace ACS_View.Converters
{
    internal class NISFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string nisNumber
                ? NisNumberRules.Format(nisNumber)
                : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string formattedNis
                ? NisNumberRules.Normalize(formattedNis)
                : value;
        }
    }
}
