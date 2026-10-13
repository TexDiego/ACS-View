namespace ACS_View.Domain.ValueObjects;

public static class WhatsAppPhone
{
    private static readonly HashSet<string> AreaCodes = new(
        "11 12 13 14 15 16 17 18 19 21 22 24 27 28 31 32 33 34 35 37 38 41 42 43 44 45 46 47 48 49 51 53 54 55 61 62 63 64 65 66 67 68 69 71 73 74 75 77 79 81 82 83 84 85 86 87 88 89 91 92 93 94 95 96 97 98 99".Split(' '));

    public static bool TryCreateChatUri(string? value, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var number = value.Trim();
        if (number.StartsWith('+')) number = number[1..];
        if (number.Any(c => !(c is >= '0' and <= '9' or '(' or ')' or '-' or '.' || char.IsWhiteSpace(c))))
            return false;

        var digits = Phone.Digits(number);
        // Local Brazilian numbers include DDD; international ones include country code 55.
        if (digits.Length is 12 or 13 && digits.StartsWith("55")) digits = digits[2..];
        if (digits.Length is not (10 or 11) || !AreaCodes.Contains(digits[..2])) return false;
        var subscriber = digits[2..];
        if (digits.Length == 11 ? subscriber[0] != '9' : subscriber[0] is < '2' or > '5') return false;
        if (subscriber.All(c => c == subscriber[0])) return false;

        uri = new Uri($"https://wa.me/55{digits}");
        return true;
    }
}
