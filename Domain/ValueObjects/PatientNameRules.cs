using System.Globalization;
using System.Text.RegularExpressions;

namespace ACS_View.Domain.ValueObjects;

public static class PatientNameRules
{
    private const string AllowedLetters =
        "A-Za-z" +
        "\\u00C0\\u00C1\\u00C2\\u00C3\\u00C7\\u00C9\\u00CA\\u00CD\\u00D3\\u00D4\\u00D5\\u00DA\\u00DC" +
        "\\u00E0\\u00E1\\u00E2\\u00E3\\u00E7\\u00E9\\u00EA\\u00ED\\u00F3\\u00F4\\u00F5\\u00FA\\u00FC";
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NameRegex = new(
        $"^[{AllowedLetters}]+(?:[ '\\u2019-][{AllowedLetters}]+)*$",
        RegexOptions.Compiled);
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    public static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} é obrigatório.");
        }

        return Normalize(value, fieldName, allowEmpty: false);
    }

    public static string NormalizeOptional(string value, string fieldName)
    {
        return Normalize(value, fieldName, allowEmpty: true);
    }

    private static string Normalize(string value, string fieldName, bool allowEmpty)
    {
        var compact = WhitespaceRegex.Replace(value.Trim(), " ");
        if (string.IsNullOrWhiteSpace(compact))
        {
            return allowEmpty ? string.Empty : throw new ArgumentException($"{fieldName} é obrigatório.");
        }

        if (!NameRegex.IsMatch(compact))
        {
            throw new ArgumentException($"{fieldName} deve conter apenas letras, acentos válidos, espaços, hífen ou apóstrofo.");
        }

        return Capitalize(compact);
    }

    private static string Capitalize(string value)
    {
        var result = new char[value.Length];
        var capitalizeNext = true;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index] == '\u2019' ? '\'' : value[index];

            if (char.IsLetter(character))
            {
                var lower = char.ToLower(character, PortugueseCulture);
                result[index] = capitalizeNext ? char.ToUpper(lower, PortugueseCulture) : lower;
                capitalizeNext = false;
                continue;
            }

            result[index] = character;
            capitalizeNext = true;
        }

        return new string(result);
    }
}
