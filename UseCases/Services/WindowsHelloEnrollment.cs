using ACS_View.Application.Interfaces;
using ACS_View.Application.Security;
using ACS_View.Domain.Entities;
using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace ACS_View.UseCases.Services;

internal sealed record WindowsHelloEnrollment(string Name, string PublicKey, int UserId, int Revision, string DatabaseIdentity, string CredentialIdentity)
{
    public static string AccountIdentity(User user) => user.Username;
    public static string CredentialIdentityOf(User user) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{user.Username}|{user.PasswordSalt}|{user.PasswordHashVersion}")));
    public static string Key(string identity, int userId) => $"WindowsHelloV1_{identity}_{userId}";
    public static async Task<WindowsHelloEnrollment?> ReadAsync(IDatabaseService db, ISecureStorage storage, int userId)
    {
        var identity = await LocalDatabaseIdentity.GetAsync(db);
        var raw = await storage.GetAsync(Key(identity, userId));
        return raw is null ? null : JsonSerializer.Deserialize<WindowsHelloEnrollment>(raw);
    }
    public bool Matches(User user, string identity) => UserId == user.Id && Revision == user.CredentialRevision
        && DatabaseIdentity == identity && CredentialIdentity == CredentialIdentityOf(user);
    public static async Task<bool> IsEnabledAsync(IDatabaseService db, ISecureStorage storage, IWindowsHelloService hello, User user)
    {
        try
        {
            var enrollment = await ReadAsync(db, storage, user.Id);
            return enrollment is not null && enrollment.Matches(user, await LocalDatabaseIdentity.GetAsync(db))
                && Convert.ToBase64String(await hello.GetPublicKeyAsync(enrollment.Name)) == enrollment.PublicKey;
        }
        catch { return false; }
    }
    public static async Task RevokeAsync(IDatabaseService db, ISecureStorage storage, IWindowsHelloService hello, int userId)
    {
        var identity = await LocalDatabaseIdentity.GetAsync(db);
        WindowsHelloEnrollment? enrollment = null;
        try { enrollment = await ReadAsync(db, storage, userId); } catch (JsonException) { }
        var key = Key(identity, userId);
        storage.Remove(key);
        // Check removal before reporting success. An orphan key has no authorization.
        if (await storage.GetAsync(key) is not null) throw new InvalidOperationException("Não foi possível revogar o Windows Hello. Tente novamente.");
        if (enrollment is not null)
        {
            try { await hello.DeleteAsync(enrollment.Name); }
            catch { System.Diagnostics.Debug.WriteLine("Chave Windows Hello órfã sem autorização local."); }
        }
    }
    public static bool Verify(byte[] publicKey, byte[] challenge, byte[] signature)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out var read);
            return read == publicKey.Length && rsa.KeySize >= 2048 && rsa.VerifyData(challenge, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
