using ACS_View.Application.Interfaces;
using ACS_View.Application.Security;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using Microsoft.Maui.Storage;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace ACS_View.UseCases.Services;

internal sealed class AuthService(IDatabaseService databaseService, ICurrentUserContext currentUserContext,
    ISecureStorage secureStorage, TimeProvider timeProvider, ILocalBiometricVault biometricVault) : IAuthService
{
    private const string SessionKey = "LocalAuthSessionV2";
    private const string BiometricAccountKey = "BiometricAccountV1";
    private readonly SemaphoreSlim gate = new(1, 1);
    private sealed record LocalSession(int UserId, int Revision, long ExpiresUtc);
    private sealed record BiometricGrant(int UserId, int Revision, string Token);

    public async Task<bool> IsAuthenticatedAsync()
    {
        await gate.WaitAsync();
        try
        {
            await databaseService.InitializeAsync();
            RemoveLegacySession();
            // A persisted password session never bypasses an enrolled biometric lock.
            if (await secureStorage.GetAsync(BiometricAccountKey) is not null || await biometricVault.HasEnrollmentAsync())
            {
                ClearSession();
                return false;
            }
            var raw = await secureStorage.GetAsync(SessionKey);
            var session = raw is null ? null : JsonSerializer.Deserialize<LocalSession>(raw);
            var user = session is null ? null : await databaseService.GetUserByIdAsync(session.UserId);
            if (session is null || user is null || session.ExpiresUtc <= Now || session.Revision != user.CredentialRevision)
            {
                ClearSession();
                return false;
            }
            await databaseService.ClaimLegacyDataForUserAsync(user.Id);
            SetContext(user.Id);
            return true;
        }
        catch
        {
            ClearSession();
            return false;
        }
        finally { gate.Release(); }
    }

    public async Task LoginAsync(string username, string password)
    {
        await gate.WaitAsync();
        try
        {
            await databaseService.InitializeAsync();
            var user = await FindUserAsync(username);
            if (user is null || password.Length > 128) throw InvalidLogin();
            EnsureNotBlocked(user.LoginBlockedUntilUtc);
            if (!await VerifyPasswordAsync(user, password))
            {
                await RecordLoginFailureAsync(user);
                throw InvalidLogin();
            }
            user.FailedLoginAttempts = 0;
            user.LoginBlockedUntilUtc = 0;
            await databaseService.UpdateUserAsync(user);
            var enrolledId = await secureStorage.GetAsync(BiometricAccountKey);
            if (enrolledId is not null && enrolledId != user.Id.ToString()) await RemoveBiometricEnrollmentAsync();
            await databaseService.ClaimLegacyDataForUserAsync(user.Id);
            await SaveSessionAsync(user);
            SetContext(user.Id);
        }
        finally { gate.Release(); }
    }

    public async Task LogoutAsync()
    {
        await gate.WaitAsync();
        try
        {
            var user = await GetCurrentUserAsync();
            if (user is not null)
            {
                user.BiometricTokenHash = string.Empty;
                await databaseService.UpdateUserAsync(user);
            }
            await RemoveBiometricEnrollmentAsync();
            ClearSession();
        }
        finally { gate.Release(); }
    }

    public Task<User?> GetCurrentUserAsync() => currentUserContext.HasCurrentUser
        ? databaseService.GetUserByIdAsync(currentUserContext.CurrentUserId)
        : Task.FromResult<User?>(null);

    // The returned code is displayed once. Only its salted hash is persisted.
    public async Task<string> RegisterAsync(string username, string password)
    {
        username = AccountSecurity.ValidateUsername(username);
        AccountSecurity.ValidatePassword(password);
        await gate.WaitAsync();
        try
        {
            await databaseService.InitializeAsync();
            if (await databaseService.GetUserByUsernameAsync(username) is not null)
                throw new InvalidOperationException("Já existe um usuário com esse nome.");
            var user = new User { Username = username, CredentialRevision = 1 };
            var code = await Task.Run(() =>
            {
                AccountSecurity.SetPassword(user, password);
                return AccountSecurity.RotateRecoveryCode(user);
            });
            await databaseService.InsertUserAsync(user);
            return code;
        }
        finally { gate.Release(); }
    }

    public async Task<string> ResetPasswordAsync(string username, string recoveryCode, string newPassword)
    {
        AccountSecurity.ValidatePassword(newPassword);
        await gate.WaitAsync();
        try
        {
            await databaseService.InitializeAsync();
            var user = await FindUserAsync(username);
            if (user is null) throw InvalidRecovery();
            EnsureNotBlocked(user.RecoveryBlockedUntilUtc);
            var normalized = AccountSecurity.NormalizeRecoveryCode(recoveryCode);
            if (normalized.Length != 32 || !await Task.Run(() => PasswordHasher.Verify(normalized, user.RecoveryCodeHash, user.RecoveryCodeSalt)))
            {
                user.FailedRecoveryAttempts++;
                if (user.FailedRecoveryAttempts >= 5)
                {
                    user.RecoveryBlockedUntilUtc = Now + 300;
                    user.FailedRecoveryAttempts = 0;
                }
                await databaseService.UpdateUserAsync(user);
                throw InvalidRecovery();
            }
            var code = await Task.Run(() =>
            {
                AccountSecurity.SetPassword(user, newPassword);
                return AccountSecurity.RotateRecoveryCode(user);
            });
            user.CredentialRevision++;
            user.BiometricTokenHash = string.Empty;
            user.FailedLoginAttempts = user.FailedRecoveryAttempts = 0;
            user.LoginBlockedUntilUtc = user.RecoveryBlockedUntilUtc = 0;
            await databaseService.UpdateUserAsync(user);
            await RemoveBiometricEnrollmentForUserAsync(user.Id);
            ClearSession();
            return code;
        }
        finally { gate.Release(); }
    }

    public async Task<string> ConfigureRecoveryAsync(string currentPassword, string newPassword)
    {
        AccountSecurity.ValidatePassword(newPassword);
        await gate.WaitAsync();
        try
        {
            var user = await GetCurrentUserAsync() ?? throw new InvalidOperationException("Entre na conta para configurar a segurança.");
            EnsureNotBlocked(user.LoginBlockedUntilUtc);
            if (currentPassword.Length > 128 || !await VerifyPasswordAsync(user, currentPassword))
            {
                await RecordLoginFailureAsync(user);
                throw InvalidLogin();
            }
            var code = await Task.Run(() =>
            {
                AccountSecurity.SetPassword(user, newPassword);
                return AccountSecurity.RotateRecoveryCode(user);
            });
            user.CredentialRevision++;
            user.BiometricTokenHash = string.Empty;
            user.FailedLoginAttempts = user.FailedRecoveryAttempts = 0;
            user.LoginBlockedUntilUtc = user.RecoveryBlockedUntilUtc = 0;
            await databaseService.UpdateUserAsync(user);
            await RemoveBiometricEnrollmentForUserAsync(user.Id);
            // Credentials are already saved. Even if the vault cannot renew the session,
            // return the only copy of the recovery code; SaveSessionAsync clears the session.
            try { await SaveSessionAsync(user); }
            catch (InvalidOperationException) { }
            return code;
        }
        finally { gate.Release(); }
    }

    public async Task<BiometricStatus> GetBiometricStatusAsync()
    {
        await gate.WaitAsync();
        try
        {
            await databaseService.InitializeAsync();
            var id = await secureStorage.GetAsync(BiometricAccountKey);
            var user = int.TryParse(id, out var userId) ? await databaseService.GetUserByIdAsync(userId) : null;
            var enabled = user is not null && !string.IsNullOrEmpty(user.BiometricTokenHash)
                && await biometricVault.HasEnrollmentAsync()
                && (!currentUserContext.HasCurrentUser || currentUserContext.CurrentUserId == user.Id);
            return new BiometricStatus(await biometricVault.IsAvailableAsync(), enabled);
        }
        catch { return new BiometricStatus(false, false); }
        finally { gate.Release(); }
    }

    public async Task ConfigureBiometricsAsync(string currentPassword, bool enable)
    {
        await gate.WaitAsync();
        try
        {
            var user = await GetCurrentUserAsync() ?? throw new InvalidOperationException("Entre com sua senha antes de configurar a biometria.");
            EnsureNotBlocked(user.LoginBlockedUntilUtc);
            if (currentPassword.Length > 128 || !await VerifyPasswordAsync(user, currentPassword))
            {
                await RecordLoginFailureAsync(user);
                throw InvalidLogin();
            }
            if (!enable)
            {
                user.BiometricTokenHash = string.Empty;
                await databaseService.UpdateUserAsync(user);
                await RemoveBiometricEnrollmentAsync();
                return;
            }
            if (!await biometricVault.IsAvailableAsync()) throw new InvalidOperationException("Biometria indisponível neste aparelho. Continue usando sua senha.");
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var payload = JsonSerializer.Serialize(new BiometricGrant(user.Id, user.CredentialRevision, token));
            try
            {
                await biometricVault.ProtectAsync(payload);
                await secureStorage.SetAsync(BiometricAccountKey, user.Id.ToString());
                user.BiometricTokenHash = TokenHash(token);
                user.FailedLoginAttempts = 0;
                user.LoginBlockedUntilUtc = 0;
                await databaseService.UpdateUserAsync(user);
            }
            catch
            {
                await RemoveBiometricEnrollmentAsync();
                throw;
            }
        }
        finally { gate.Release(); }
    }

    public async Task LoginWithBiometricsAsync()
    {
        await gate.WaitAsync();
        try
        {
            await databaseService.InitializeAsync();
            // UnlockAsync returns plaintext only after the OS authorizes the cryptographic key.
            var payload = await biometricVault.UnlockAsync();
            BiometricGrant? grant;
            try { grant = JsonSerializer.Deserialize<BiometricGrant>(payload); }
            catch (JsonException) { grant = null; }
            var user = grant is null ? null : await databaseService.GetUserByIdAsync(grant.UserId);
            var enrolledId = await secureStorage.GetAsync(BiometricAccountKey);
            if (grant is null || user is null || enrolledId != user.Id.ToString()
                || user.CredentialRevision != grant.Revision || string.IsNullOrEmpty(user.BiometricTokenHash)
                || grant.Token is null || grant.Token.Length != 44 || !ValidBiometricToken(grant.Token, user.BiometricTokenHash))
            {
                await RemoveBiometricEnrollmentAsync();
                ClearSession();
                throw new InvalidOperationException("A autorização biométrica expirou. Entre com a senha e ative-a novamente.");
            }
            EnsureNotBlocked(user.LoginBlockedUntilUtc);
            await databaseService.ClaimLegacyDataForUserAsync(user.Id);
            await SaveSessionAsync(user);
            SetContext(user.Id);
        }
        finally { gate.Release(); }
    }

    private static string TokenHash(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static bool ValidBiometricToken(string token, string expected)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(TokenHash(token)), Convert.FromBase64String(expected));
        }
        catch (FormatException) { return false; }
    }
    private async Task RemoveBiometricEnrollmentForUserAsync(int id)
    {
        if (await secureStorage.GetAsync(BiometricAccountKey) == id.ToString()) await RemoveBiometricEnrollmentAsync();
    }
    private async Task RemoveBiometricEnrollmentAsync()
    {
        secureStorage.Remove(BiometricAccountKey);
        // Database token revocation remains authoritative if the platform key cannot be deleted.
        try { await biometricVault.RemoveAsync(); }
        catch { System.Diagnostics.Debug.WriteLine("Não foi possível remover a chave biométrica local."); }
    }

    private async Task RecordLoginFailureAsync(User user)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= 5)
        {
            user.LoginBlockedUntilUtc = Now + 300;
            user.FailedLoginAttempts = 0;
        }
        await databaseService.UpdateUserAsync(user);
    }

    private async Task<bool> VerifyPasswordAsync(User user, string password)
    {
        // Never compare against the empty legacy column of an already hashed account.
        var valid = await Task.Run(() => PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt, user.PasswordHashVersion));
        if (!valid) return false;
        if (user.PasswordHashVersion != PasswordHasher.CurrentVersion)
        {
            await Task.Run(() => AccountSecurity.SetPassword(user, password));
            await databaseService.UpdateUserAsync(user);
        }
        return true;
    }

    private Task<User?> FindUserAsync(string username) => string.IsNullOrWhiteSpace(username) || username.Length > 64
        ? Task.FromResult<User?>(null) : databaseService.GetUserByUsernameAsync(username.Trim());

    private long Now => timeProvider.GetUtcNow().ToUnixTimeSeconds();
    private void EnsureNotBlocked(long until)
    {
        if (until > Now) throw new InvalidOperationException("Muitas tentativas. Aguarde alguns minutos e tente novamente.");
    }
    private static Exception InvalidLogin() => new InvalidOperationException("Usuário ou senha inválidos.");
    private static Exception InvalidRecovery() => new InvalidOperationException("Usuário ou código de recuperação inválidos.");
    private async Task SaveSessionAsync(User user)
    {
        try
        {
            RemoveLegacySession();
            await secureStorage.SetAsync(SessionKey, JsonSerializer.Serialize(new LocalSession(user.Id, user.CredentialRevision, Now + 7 * 86400)));
        }
        catch
        {
            ClearSession();
            throw new InvalidOperationException("Não foi possível proteger a sessão neste aparelho. Tente entrar novamente.");
        }
    }
    private void RemoveLegacySession()
    {
        secureStorage.Remove("AuthToken");
        secureStorage.Remove("AuthUserId");
        Preferences.Remove("AuthToken");
        Preferences.Remove("AuthUserId");
    }
    private void ClearSession()
    {
        secureStorage.Remove(SessionKey);
        RemoveLegacySession();
        currentUserContext.Clear();
        DataChangeTracker.MarkSessionChanged();
        DataChangeTracker.MarkAllChanged();
    }
    private void SetContext(int id)
    {
        currentUserContext.SetCurrentUser(id);
        DataChangeTracker.MarkSessionChanged();
        DataChangeTracker.MarkAllChanged();
    }
}
