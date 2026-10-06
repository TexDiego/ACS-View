using System.Globalization;

namespace ACS_View.Converters
{
    internal class SUSFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string susNumbers)
                return string.Join("; ", ACS_View.Domain.ValueObjects.SusNumberSet.Parse(susNumbers)
                    .Select(number => number.Length == 15
                        ? $"{number[..3]}.{number.Substring(3, 4)}.{number.Substring(7, 4)}.{number.Substring(11, 4)}"
                        : number));
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string formattedSUS)
                return formattedSUS.Replace(" ", "");
            return value;
        }
    }
}
