using System.Security.Cryptography;

namespace ACS_View.Application.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    public const int CurrentVersion = 2;
    private const int Iterations = 600_000;

    public static (string Hash, string Salt) Hash(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(secret, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string secret, string hash, string salt, int version = CurrentVersion)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
        {
            return false;
        }

        if (version is not (1 or CurrentVersion)) return false;
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expectedHash = Convert.FromBase64String(hash);
            if (saltBytes.Length != SaltSize || expectedHash.Length != HashSize) return false;
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(secret, saltBytes,
                version == 1 ? 100_000 : Iterations, HashAlgorithmName.SHA256, HashSize);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException) { return false; }
    }
}
