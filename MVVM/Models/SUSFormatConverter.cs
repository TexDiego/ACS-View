using System.Globalization;

namespace ACS_View.MVVM.Models
{
    public class SUSFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string susNumber && susNumber.Length == 15)
            {
                return $"{susNumber.Substring(0, 3)}.{susNumber.Substring(3, 4)}.{susNumber.Substring(7, 4)}.{susNumber.Substring(11, 4)}";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string formattedSUS)
            {
                return formattedSUS.Replace(" ", "");
            }
            return value;
        }
    }
}
