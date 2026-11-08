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
    ISecureStorage secureStorage, TimeProvider timeProvider, ILocalBiometricVault biometricVault, IWindowsHelloService? windowsHello = null) : IAuthService
{
    private readonly IWindowsHelloService hello = windowsHello ?? new ACS_View.Infrastructure.Services.UnavailableWindowsHelloService();
    private const string SessionKey = "LocalAuthSessionV2";
    private const string BiometricAccountKey = "BiometricAccountV1";
    private readonly SemaphoreSlim gate = LocalAuthenticationGate.For(databaseService);
    private sealed record LocalSession(int UserId, int Revision, long ExpiresUtc, string? DatabaseIdentity=null);
    private sealed record BiometricGrant(int UserId, int Revision, string Token, string? DatabaseIdentity=null);
    // Restores always use a new path. Identical user IDs/revisions in another snapshot
    // must never inherit a protected session or biometric authorization.
    private string DatabaseIdentity => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(databaseService.Connection.DatabasePath))));
    private string? windowsDatabaseIdentity;
    private bool sessionRevoked;
    private async Task PrepareDatabaseAsync()
    {
        await databaseService.InitializeAsync();
        if (OperatingSystem.IsWindows()) windowsDatabaseIdentity = await LocalDatabaseIdentity.GetAsync(databaseService);
    }
    private bool IsSameDatabase(string? identity) => identity==(windowsDatabaseIdentity ?? DatabaseIdentity) ||
        (!OperatingSystem.IsWindows() && identity==null && Path.GetFileName(databaseService.Connection.DatabasePath).Equals("health_app.db",StringComparison.Ordinal));

    public async Task<bool> IsAuthenticatedAsync()
    {
        await gate.WaitAsync();
        try
        {
            await PrepareDatabaseAsync();
            if (sessionRevoked) { ClearSession(); return false; }
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
            if (session is null || user is null || session.ExpiresUtc <= Now || session.Revision != user.CredentialRevision || !IsSameDatabase(session.DatabaseIdentity))
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
        if (!await gate.WaitAsync(0)) throw new InvalidOperationException("Uma autenticação já está em andamento.");
        try
        {
            await PrepareDatabaseAsync();
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
            await CompleteLoginAsync(user);
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
        }
        finally { ClearSession(); gate.Release(); }
    }

    public Task<User?> GetCurrentUserAsync() => currentUserContext.HasCurrentUser
        ? databaseService.GetUserByIdAsync(currentUserContext.CurrentUserId)
        : Task.FromResult<User?>(null);

    public async Task VerifyCurrentPasswordAsync(string password)
    {
        await gate.WaitAsync();
        try
        {
            await PrepareDatabaseAsync();
            var user=await GetCurrentUserAsync() ?? throw InvalidLogin();
            EnsureNotBlocked(user.LoginBlockedUntilUtc);
            if(password.Length>128 || !await VerifyPasswordAsync(user,password))
            {
                await RecordLoginFailureAsync(user);
                throw InvalidLogin();
            }
        }
        finally { gate.Release(); }
    }

    // The returned code is displayed once. Only its salted hash is persisted.
    public async Task<string> RegisterAsync(string username, string password)
    {
        username = AccountSecurity.ValidateUsername(username);
        AccountSecurity.ValidatePassword(password);
        await gate.WaitAsync();
        try
        {
            await PrepareDatabaseAsync();
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
            await PrepareDatabaseAsync();
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
            await RevokeHelloAfterCredentialChangeAsync(user.Id);
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
            await RevokeHelloAfterCredentialChangeAsync(user.Id);
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
            await PrepareDatabaseAsync();
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
            var payload = JsonSerializer.Serialize(new BiometricGrant(user.Id, user.CredentialRevision, token,windowsDatabaseIdentity ?? DatabaseIdentity));
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
            await PrepareDatabaseAsync();
            // UnlockAsync returns plaintext only after the OS authorizes the cryptographic key.
            var payload = await biometricVault.UnlockAsync();
            BiometricGrant? grant;
            try { grant = JsonSerializer.Deserialize<BiometricGrant>(payload); }
            catch (JsonException) { grant = null; }
            var user = grant is null ? null : await databaseService.GetUserByIdAsync(grant.UserId);
            var enrolledId = await secureStorage.GetAsync(BiometricAccountKey);
            if (grant is null || user is null || enrolledId != user.Id.ToString()
                || !IsSameDatabase(grant.DatabaseIdentity)
                || user.CredentialRevision != grant.Revision || string.IsNullOrEmpty(user.BiometricTokenHash)
                || grant.Token is null || grant.Token.Length != 44 || !ValidBiometricToken(grant.Token, user.BiometricTokenHash))
            {
                await RemoveBiometricEnrollmentAsync();
                ClearSession();
                throw new InvalidOperationException("A autorização biométrica expirou. Entre com a senha e ative-a novamente.");
            }
            EnsureNotBlocked(user.LoginBlockedUntilUtc);
            await CompleteLoginAsync(user);
        }
        finally { gate.Release(); }
    }


    private async Task CompleteLoginAsync(User user)
    {
        await databaseService.ClaimLegacyDataForUserAsync(user.Id);
        await SaveSessionAsync(user);
        SetContext(user.Id);
    }

    public async Task<WindowsHelloStatus> GetWindowsHelloStatusAsync(int? userId = null)
    {
        try
        {
            await PrepareDatabaseAsync();
            var id = userId ?? currentUserContext.CurrentUserId;
            var user = await databaseService.GetUserByIdAsync(id);
            return new(await hello.GetAvailabilityAsync(), user is not null && await WindowsHelloEnrollment.IsEnabledAsync(databaseService, secureStorage, hello, user));
        }
        catch { return new(WindowsHelloAvailability.Unavailable, false); }
    }

    public async Task ConfigureWindowsHelloAsync(string currentPassword, bool enable)
    {
        if (!await gate.WaitAsync(0)) throw new InvalidOperationException("Uma autenticação já está em andamento.");
        try
        {
            await PrepareDatabaseAsync();
            var user = await GetCurrentUserAsync() ?? throw InvalidLogin();
            EnsureNotBlocked(user.LoginBlockedUntilUtc);
            if (currentPassword.Length > 128 || !await VerifyPasswordAsync(user, currentPassword))
            {
                await RecordLoginFailureAsync(user);
                throw InvalidLogin();
            }
            if (!enable)
            {
                await WindowsHelloEnrollment.RevokeAsync(databaseService, secureStorage, hello, user.Id);
                return;
            }
            var availability = await hello.GetAvailabilityAsync();
            if (availability != WindowsHelloAvailability.Available) throw new InvalidOperationException(new WindowsHelloStatus(availability, false).Message);
            var identity = await LocalDatabaseIdentity.GetAsync(databaseService);
            // Random name prevents replacing any other account's native key.
            var name = $"ACSView-{Guid.NewGuid():N}";
            var committed = false;
            try
            {
                var publicKey = await hello.CreateAsync(name);
                var challenge = RandomNumberGenerator.GetBytes(32);
                if (!WindowsHelloEnrollment.Verify(publicKey, challenge, await hello.SignAsync(name, challenge)))
                    throw new InvalidOperationException("Não foi possível validar a credencial Windows Hello.");
                var fresh = await databaseService.GetUserByIdAsync(user.Id);
                if (fresh is null || fresh.CredentialRevision != user.CredentialRevision || !currentUserContext.HasCurrentUser || currentUserContext.CurrentUserId != user.Id)
                    throw InvalidLogin();
                await WindowsHelloEnrollment.RevokeAsync(databaseService, secureStorage, hello, user.Id);
                var enrollment = new WindowsHelloEnrollment(name, Convert.ToBase64String(publicKey), user.Id, user.CredentialRevision, identity, WindowsHelloEnrollment.CredentialIdentityOf(user));
                var key = WindowsHelloEnrollment.Key(identity, user.Id);
                var json = JsonSerializer.Serialize(enrollment);
                await secureStorage.SetAsync(key, json);
                if (await secureStorage.GetAsync(key) != json) throw new InvalidOperationException("Não foi possível proteger a autorização Windows Hello.");
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    try { await hello.DeleteAsync(name); } catch { }
                }
            }
        }
        finally { gate.Release(); }
    }

    public async Task LoginWithWindowsHelloAsync(int userId)
    {
        if (!await gate.WaitAsync(0)) throw new InvalidOperationException("Uma autenticação já está em andamento.");
        try
        {
            await PrepareDatabaseAsync();
            var user = await databaseService.GetUserByIdAsync(userId) ?? throw InvalidLogin();
            EnsureNotBlocked(user.LoginBlockedUntilUtc);
            var availability = await hello.GetAvailabilityAsync();
            if (availability != WindowsHelloAvailability.Available) throw new InvalidOperationException(new WindowsHelloStatus(availability, false).Message);
            var enrollment = await WindowsHelloEnrollment.ReadAsync(databaseService, secureStorage, userId);
            var identity = await LocalDatabaseIdentity.GetAsync(databaseService);
            if (enrollment is null || !enrollment.Matches(user, identity)) throw ExpiredHello();
            var publicKey = await hello.GetPublicKeyAsync(enrollment.Name);
            if (Convert.ToBase64String(publicKey) != enrollment.PublicKey) throw ExpiredHello();
            // Challenge is created and consumed entirely in this invocation. No caller can replay it.
            var nonce = RandomNumberGenerator.GetBytes(32);
            var signature = await hello.SignAsync(enrollment.Name, nonce);
            if (!WindowsHelloEnrollment.Verify(publicKey, nonce, signature)) throw ExpiredHello();
            var fresh = await databaseService.GetUserByIdAsync(userId);
            var current = await WindowsHelloEnrollment.ReadAsync(databaseService, secureStorage, userId);
            if (fresh is null || current != enrollment || !enrollment.Matches(fresh, await LocalDatabaseIdentity.GetAsync(databaseService))) throw ExpiredHello();
            EnsureNotBlocked(fresh.LoginBlockedUntilUtc);
            await CompleteLoginAsync(fresh);
        }
        finally { gate.Release(); }
    }
    private async Task RevokeHelloAfterCredentialChangeAsync(int userId)
    {
        if (!OperatingSystem.IsWindows()) return;
        try { await WindowsHelloEnrollment.RevokeAsync(databaseService, secureStorage, hello, userId); }
        catch { /* The new CredentialRevision rejects old authorization even if vault cleanup fails. */ }
    }
    private static Exception ExpiredHello() => new InvalidOperationException("A autorização Windows Hello expirou ou foi removida. Entre com a senha e ative-a novamente.");

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
            await secureStorage.SetAsync(SessionKey, JsonSerializer.Serialize(new LocalSession(user.Id, user.CredentialRevision, Now + 7 * 86400,windowsDatabaseIdentity ?? DatabaseIdentity)));
            sessionRevoked = false;
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
        // Clear the in-memory authorization even when the platform vault is failing.
        sessionRevoked = true;
        currentUserContext.Clear();
        try { secureStorage.Remove(SessionKey); } catch { }
        try { RemoveLegacySession(); } catch { }
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
