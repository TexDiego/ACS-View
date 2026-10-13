using ACS_View.Domain.ValueObjects;

internal static class WhatsAppPhoneTests
{
    public static void Run(Action<bool, string> check)
    {
        foreach (var number in new[] { "(11) 91234-5678", "11912345678", "+55 (11) 91234-5678", "5511912345678", "(55) 91234-5678" })
        {
            var expected = number == "(55) 91234-5678" ? "https://wa.me/5555912345678" : "https://wa.me/5511912345678";
            check(WhatsAppPhone.TryCreateChatUri(number, out var uri) && uri!.AbsoluteUri == expected, "WhatsApp normalizes DDD and country code: " + number);
        }
        check(WhatsAppPhone.TryCreateChatUri("(21) 2345-6789", out var landline) && landline!.AbsoluteUri == "https://wa.me/552123456789", "WhatsApp accepts valid landline format");
        foreach (var number in new string?[] { null, "", "912345678", "0011912345678", "(20) 91234-5678", "(11) 81234-5678", "(11) 1234-5678", "(11) 99999-9999", "11912345678 ramal 2", "abc11912345678", "11912345678/21987654321", "+1 202 555 0123" })
            check(!WhatsAppPhone.TryCreateChatUri(number, out var uri) && uri is null, "WhatsApp rejects invalid or ambiguous number: " + number);
    }
}
