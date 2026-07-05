namespace ACS_View.Domain.ValueObjects;

public static class CepNumberRules
{
    public static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }

    public static bool IsValid(string? value)
    {
        return Normalize(value).Length == 8;
    }
}
