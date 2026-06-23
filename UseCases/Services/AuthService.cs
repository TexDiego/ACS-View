using ACS_View.Application.Interfaces;
using ACS_View.Application.Security;
using ACS_View.Domain.Entities;
using System.Security.Cryptography;

namespace ACS_View.UseCases.Services;

internal sealed class AuthService(IDatabaseService databaseService) : IAuthService
{
    private const string AuthTokenKey = "AuthToken";
    private const int HashVersion = 1;

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (!string.IsNullOrWhiteSpace(await SecureStorage.Default.GetAsync(AuthTokenKey)))
        {
            return true;
        }

        var legacyToken = Preferences.Get(AuthTokenKey, string.Empty);
        if (string.IsNullOrWhiteSpace(legacyToken))
        {
            return false;
        }

        await SecureStorage.Default.SetAsync(AuthTokenKey, legacyToken);
        Preferences.Remove(AuthTokenKey);
        return true;
    }

    public async Task LoginAsync(string username, string password)
    {
        var user = await GetUserOrThrowAsync(username);
        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt) &&
            !await TryMigrateLegacyPasswordAsync(user, password))
        {
            throw new InvalidOperationException("Usuário ou senha inválidos.");
        }

        await SecureStorage.Default.SetAsync(AuthTokenKey, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public Task LogoutAsync()
    {
        SecureStorage.Default.Remove(AuthTokenKey);
        Preferences.Remove(AuthTokenKey);
        return Task.CompletedTask;
    }

    public async Task RegisterAsync(string username, string password, string securityQuestion, string securityAnswer)
    {
        username = NormalizeRequired(username, "Usuário");
        password = NormalizeRequired(password, "Senha");
        securityQuestion = NormalizeRequired(securityQuestion, "Pergunta de segurança");
        securityAnswer = NormalizeRequired(securityAnswer, "Resposta de segurança");

        if (await databaseService.GetUserByUsernameAsync(username) is not null)
        {
            throw new InvalidOperationException("Já existe um usuário com esse nome.");
        }

        var passwordHash = PasswordHasher.Hash(password);
        var answerHash = PasswordHasher.Hash(NormalizeSecurityAnswer(securityAnswer));

        await databaseService.InsertUserAsync(new User
        {
            Username = username,
            Password = string.Empty,
            PasswordHash = passwordHash.Hash,
            PasswordSalt = passwordHash.Salt,
            PasswordHashVersion = HashVersion,
            SecurityQuestion = securityQuestion,
            SecurityAnswer = string.Empty,
            SecurityAnswerHash = answerHash.Hash,
            SecurityAnswerSalt = answerHash.Salt
        });
    }

    public async Task<User?> GetUserForPasswordRecoveryAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await databaseService.GetUserByUsernameAsync(username.Trim());
    }

    public async Task ResetPasswordAsync(string username, string securityAnswer, string newPassword)
    {
        var user = await GetUserOrThrowAsync(username);
        var normalizedAnswer = NormalizeSecurityAnswer(securityAnswer);

        if (!PasswordHasher.Verify(normalizedAnswer, user.SecurityAnswerHash, user.SecurityAnswerSalt) &&
            !await TryMigrateLegacySecurityAnswerAsync(user, normalizedAnswer))
        {
            throw new InvalidOperationException("Resposta de segurança incorreta.");
        }

        var passwordHash = PasswordHasher.Hash(NormalizeRequired(newPassword, "Nova senha"));
        user.Password = string.Empty;
        user.PasswordHash = passwordHash.Hash;
        user.PasswordSalt = passwordHash.Salt;
        user.PasswordHashVersion = HashVersion;

        await databaseService.UpdateUserAsync(user);
    }

    private async Task<User> GetUserOrThrowAsync(string username)
    {
        username = NormalizeRequired(username, "Usuário");
        return await databaseService.GetUserByUsernameAsync(username)
            ?? throw new InvalidOperationException("Usuário ou senha inválidos.");
    }

    private async Task<bool> TryMigrateLegacyPasswordAsync(User user, string password)
    {
        if (!string.Equals(user.Password, password, StringComparison.Ordinal))
        {
            return false;
        }

        var passwordHash = PasswordHasher.Hash(password);
        user.Password = string.Empty;
        user.PasswordHash = passwordHash.Hash;
        user.PasswordSalt = passwordHash.Salt;
        user.PasswordHashVersion = HashVersion;
        await databaseService.UpdateUserAsync(user);
        return true;
    }

    private async Task<bool> TryMigrateLegacySecurityAnswerAsync(User user, string normalizedAnswer)
    {
        if (string.IsNullOrWhiteSpace(user.SecurityAnswer))
        {
            return false;
        }

        if (!string.Equals(NormalizeSecurityAnswer(user.SecurityAnswer), normalizedAnswer, StringComparison.Ordinal))
        {
            return false;
        }

        var answerHash = PasswordHasher.Hash(normalizedAnswer);
        user.SecurityAnswer = string.Empty;
        user.SecurityAnswerHash = answerHash.Hash;
        user.SecurityAnswerSalt = answerHash.Salt;
        await databaseService.UpdateUserAsync(user);
        return true;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} é obrigatório.");
        }

        return value.Trim();
    }

    private static string NormalizeSecurityAnswer(string value)
    {
        return NormalizeRequired(value, "Resposta de segurança").ToUpperInvariant();
    }
}
