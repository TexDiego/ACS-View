using System.Diagnostics;
using System.Globalization;

namespace ACS_View.Converters
{
    internal class StatusToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string normalizedStatus = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalizedStatus is string status)
            {
                return status switch
                {
                    "realizada" => ThemeColors.Success,
                    "ausente" => ThemeColors.Warning,
                    "recusada" => ThemeColors.Danger,
                    _ => Colors.Gray
                };
            }
            return null;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}