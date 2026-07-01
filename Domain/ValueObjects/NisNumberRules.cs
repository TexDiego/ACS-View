namespace ACS_View.Domain.ValueObjects;

public static class NisNumberRules
{
    public const int DigitsLength = 11;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }

    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);
        if (digits.Length != DigitsLength)
        {
            return false;
        }

        if (digits.All(digit => digit == digits[0]))
        {
            return false;
        }

        var weights = new[] { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var sum = 0;

        for (var index = 0; index < weights.Length; index++)
        {
            sum += (digits[index] - '0') * weights[index];
        }

        var remainder = sum % 11;
        var checkDigit = 11 - remainder;
        if (checkDigit is 10 or 11)
        {
            checkDigit = 0;
        }

        return checkDigit == digits[^1] - '0';
    }

    public static string Format(string? value)
    {
        var digits = Normalize(value);
        if (digits.Length == 0)
        {
            return string.Empty;
        }

        var first = TakeDigits(digits, 0, 3);
        var second = TakeDigits(digits, 3, 5);
        var third = TakeDigits(digits, 8, 2);
        var fourth = TakeDigits(digits, 10, 1);

        var formatted = first;
        if (second.Length > 0)
        {
            formatted += $".{second}";
        }

        if (third.Length > 0)
        {
            formatted += $".{third}";
        }

        if (fourth.Length > 0)
        {
            formatted += $"-{fourth}";
        }

        return formatted;
    }

    private static string TakeDigits(string digits, int startIndex, int length)
    {
        if (digits.Length <= startIndex)
        {
            return string.Empty;
        }

        return digits.Substring(startIndex, Math.Min(length, digits.Length - startIndex));
    }
}
