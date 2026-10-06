using System.Security.Cryptography;
using ACS_View.Domain.Entities;

namespace ACS_View.Application.Security;

public static class AccountSecurity
{
    public static string ValidateUsername(string username)
    {
        username = username.Trim();
        if (username.Length is < 3 or > 64 || username.Any(char.IsControl))
            throw new InvalidOperationException("Use um usuário de 3 a 64 caracteres, sem caracteres de controle.");
        return username;
    }

    public static void ValidatePassword(string password)
    {
        if (password.Length is < 15 or > 128 || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Use uma senha de 15 a 128 caracteres. Uma frase com várias palavras é mais fácil de lembrar.");
    }

    public static string NewRecoveryCode()
    {
        var hex = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        return string.Join("-", Enumerable.Range(0, 8).Select(i => hex.Substring(i * 4, 4)));
    }

    public static string NormalizeRecoveryCode(string code) => code.Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();

    public static void SetPassword(User user, string password)
    {
        var hash = PasswordHasher.Hash(password);
        user.PasswordHash = hash.Hash;
        user.PasswordSalt = hash.Salt;
        user.PasswordHashVersion = PasswordHasher.CurrentVersion;
        user.Password = string.Empty;
    }

    public static string RotateRecoveryCode(User user)
    {
        var code = NewRecoveryCode();
        var hash = PasswordHasher.Hash(NormalizeRecoveryCode(code));
        user.RecoveryCodeHash = hash.Hash;
        user.RecoveryCodeSalt = hash.Salt;
        ClearSecurityQuestion(user);
        return code;
    }

    public static void ClearSecurityQuestion(User user)
    {
        user.SecurityQuestion = user.SecurityAnswer = user.SecurityAnswerHash = user.SecurityAnswerSalt = string.Empty;
    }
}
