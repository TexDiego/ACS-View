using System.Text.RegularExpressions;

namespace ACS_View.Domain.ValueObjects;

public static class SusNumberSet
{
    // Dots, hyphens and spaces format a CNS; explicit separators split numbers.
    public static IReadOnlyList<string> Parse(string? value) =>
        (value ?? string.Empty).Split([';', ',', '/', '|', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(number => Regex.Replace(number, @"\D", string.Empty))
            .Where(number => number.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public static string Join(IEnumerable<string> numbers) =>
        string.Join("; ", numbers.SelectMany(Parse).Distinct(StringComparer.Ordinal));

    public static bool Overlaps(string? left, string? right) =>
        Parse(left).Intersect(Parse(right), StringComparer.Ordinal).Any();
}
