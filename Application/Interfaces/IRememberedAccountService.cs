namespace ACS_View.Application.Interfaces;

public sealed record RememberedAccount(int UserId, string Username, DateTimeOffset LastUsedUtc, bool WindowsHelloEnabled);
public interface IRememberedAccountService
{
    Task<IReadOnlyList<RememberedAccount>> GetAccountsAsync();
    Task<RememberedAccount?> GetLastUsedAsync();
    Task<bool> IsRememberedAsync(int userId);
    // Must follow a successful authentication; the service checks the current context.
    Task RememberCurrentAsync(bool consent);
    Task ForgetAsync(int userId);
}
