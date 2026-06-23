using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task LoginAsync(string username, string password);
    Task LogoutAsync();
    Task RegisterAsync(string username, string password, string securityQuestion, string securityAnswer);
    Task<User?> GetUserForPasswordRecoveryAsync(string username);
    Task ResetPasswordAsync(string username, string securityAnswer, string newPassword);
}
