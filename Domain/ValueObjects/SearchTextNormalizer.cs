using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ACS_View.Domain.ValueObjects;

public static class SearchTextNormalizer
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var compact = WhitespaceRegex.Replace(value.Trim(), " ");
        var normalized = compact.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
