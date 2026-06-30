using System.Globalization;

namespace ACS_View.Converters
{
    internal class CIDFormatConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string cid)
                return value;

            cid = cid.Trim();

            if (cid.Length < 4)
                return value;

            if (!IsCID(cid[..4]))
                return value;

            if (cid.Length == 4 || cid[4] == ' ')
                return $"{cid[..3]}.{cid[3..]}";

            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value;
        }

        private static bool IsCID(string value)
        {
            return value.Length == 4
                && char.IsLetter(value[0])
                && char.IsDigit(value[1])
                && char.IsDigit(value[2])
                && char.IsDigit(value[3]);
        }
    }
}