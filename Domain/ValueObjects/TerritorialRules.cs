using ACS_View.Domain.Entities;
using System.Globalization;

namespace ACS_View.Domain.ValueObjects;

public enum HouseParity { All, Even, Odd }

public static class TerritorialRules
{
    public static string? OptionalCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static bool IsClassified(House house) => OptionalCode(house.SetorCensitario) != null || OptionalCode(house.NumeroQuadra) != null;
    public static bool IsComplete(House house) => OptionalCode(house.SetorCensitario) != null && OptionalCode(house.NumeroQuadra) != null;
    public static string Street(House house) => string.Join(" ", new[] { house.TipoLogradouro, house.Rua }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    public static string Address(House house) => $"{Street(house)}, {house.NumeroCasa}" + (string.IsNullOrWhiteSpace(house.Complemento) ? "" : $" - {house.Complemento}");

    // Only an initial decimal component is an address number; S/N and Fundos remain unknown.
    public static int? NumericPart(string? value)
    {
        var text = value?.Trim() ?? "";
        var length = 0;
        while (length < text.Length && text[length] is >= '0' and <= '9') length++;
        return length > 0 && int.TryParse(text.AsSpan(0, length), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    public static bool MatchesNumber(string? value, int? start, int? end, HouseParity parity)
    {
        if (start == null && end == null && parity == HouseParity.All) return true;
        var number = NumericPart(value);
        return number.HasValue && (!start.HasValue || number >= start) && (!end.HasValue || number <= end)
            && (parity == HouseParity.All || number % 2 == (parity == HouseParity.Even ? 0 : 1));
    }

    public static void ValidateRange(int? start, int? end)
    {
        if (start < 0 || end < 0 || (start.HasValue && end.HasValue && start > end))
            throw new ArgumentException("Informe um intervalo válido: o número inicial deve ser menor ou igual ao final.");
    }
}

// Compare digit runs by magnitude without parsing into a bounded integer. Distinct codes retain their zeros.
public sealed class NaturalCodeComparer : IComparer<string?>
{
    public static NaturalCodeComparer Instance { get; } = new();
    public int Compare(string? x, string? y)
    {
        x ??= ""; y ??= "";
        int a = 0, b = 0;
        while (a < x.Length && b < y.Length)
        {
            if (char.IsAsciiDigit(x[a]) && char.IsAsciiDigit(y[b]))
            {
                int ae = a, be = b;
                while (ae < x.Length && char.IsAsciiDigit(x[ae])) ae++;
                while (be < y.Length && char.IsAsciiDigit(y[be])) be++;
                var nx = x[a..ae].TrimStart('0'); var ny = y[b..be].TrimStart('0');
                var c = nx.Length.CompareTo(ny.Length);
                if (c == 0) c = string.CompareOrdinal(nx, ny);
                if (c != 0) return c;
                a = ae; b = be;
            }
            else
            {
                var c = char.ToUpperInvariant(x[a]).CompareTo(char.ToUpperInvariant(y[b]));
                if (c != 0) return c;
                a++; b++;
            }
        }
        var result = (x.Length - a).CompareTo(y.Length - b);
        return result != 0 ? result : string.CompareOrdinal(x, y);
    }
}
