using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    Task<string> RegisterAsync(string username, string password);
    Task<string> ResetPasswordAsync(string username, string recoveryCode, string newPassword);
    Task<string> ConfigureRecoveryAsync(string currentPassword, string newPassword);
    Task<BiometricStatus> GetBiometricStatusAsync();
    Task ConfigureBiometricsAsync(string currentPassword, bool enable);
    Task LoginWithBiometricsAsync();
}
