using ACS_View.Application.Interfaces;
using ACS_View.Application.Security;
using Microsoft.Maui.Storage;
using System.Text.Json;
namespace ACS_View.UseCases.Services;

internal sealed class RememberedAccountService(IDatabaseService database, ICurrentUserContext context,
    ISecureStorage storage, TimeProvider clock, IWindowsHelloService hello) : IRememberedAccountService
{
    private const string Key = "RememberedWindowsAccountsV1";
    private readonly SemaphoreSlim gate = new(1, 1);
    private sealed record Entry(int UserId, string Username, DateTimeOffset LastUsedUtc, string DatabaseIdentity, string AccountIdentity);
    private async Task<List<Entry>> ReadAsync()
    {
        var raw = await storage.GetAsync(Key);
        return raw is null ? [] : JsonSerializer.Deserialize<List<Entry>>(raw) ?? [];
    }
    public async Task<IReadOnlyList<RememberedAccount>> GetAccountsAsync()
    {
        await gate.WaitAsync();
        try
        {
            var identity = await LocalDatabaseIdentity.GetAsync(database);
            var result = new List<RememberedAccount>();
            foreach (var entry in (await ReadAsync()).Where(e => e.DatabaseIdentity == identity).DistinctBy(e => e.UserId))
            {
                var user = await database.GetUserByIdAsync(entry.UserId);
                if (user is null || user.Username != entry.Username || WindowsHelloEnrollment.AccountIdentity(user) != entry.AccountIdentity) continue;
                var enabled = await WindowsHelloEnrollment.IsEnabledAsync(database, storage, hello, user);
                result.Add(new(user.Id, user.Username, entry.LastUsedUtc, enabled));
            }
            return result.OrderByDescending(e => e.LastUsedUtc).ToArray();
        }
        catch { return Array.Empty<RememberedAccount>(); } // Optional convenience never blocks password login.
        finally { gate.Release(); }
    }
    public async Task<RememberedAccount?> GetLastUsedAsync() => (await GetAccountsAsync()).FirstOrDefault();
    public async Task<bool> IsRememberedAsync(int userId) => (await GetAccountsAsync()).Any(e => e.UserId == userId);
    public async Task RememberCurrentAsync(bool consent)
    {
        await gate.WaitAsync();
        try
        {
            if (!context.HasCurrentUser) return;
            var identity = await LocalDatabaseIdentity.GetAsync(database);
            var user = await database.GetUserByIdAsync(context.CurrentUserId);
            if (user is null) return;
            List<Entry> entries;
            try { entries = await ReadAsync(); } catch (JsonException) { entries = []; }
            var exists = entries.Any(e => e.UserId == user.Id && e.DatabaseIdentity == identity);
            if (!consent && !exists) return;
            entries.RemoveAll(e => e.UserId == user.Id && e.DatabaseIdentity == identity);
            entries.Add(new(user.Id, user.Username, clock.GetUtcNow(), identity, WindowsHelloEnrollment.AccountIdentity(user)));
            await storage.SetAsync(Key, JsonSerializer.Serialize(entries));
        }
        catch { /* No authenticated data or session depends on this list. */ }
        finally { gate.Release(); }
    }
    public async Task ForgetAsync(int userId)
    {
        var authGate = LocalAuthenticationGate.For(database);
        if (!await authGate.WaitAsync(0)) throw new InvalidOperationException("Aguarde a autenticação em andamento antes de esquecer a conta.");
        await gate.WaitAsync();
        try
        {
            var identity = await LocalDatabaseIdentity.GetAsync(database);
            // Remove authorization before updating the selector; deletion failures fail closed.
            await WindowsHelloEnrollment.RevokeAsync(database, storage, hello, userId);
            var entries = await ReadAsync();
            entries.RemoveAll(e => e.UserId == userId && e.DatabaseIdentity == identity);
            await storage.SetAsync(Key, JsonSerializer.Serialize(entries));
        }
        finally { gate.Release(); authGate.Release(); }
    }
}
