namespace Microsoft.Maui.Storage;

// Only the platform vault is substituted. AuthService and SQLite migrations are real.
public interface ISecureStorage
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    bool Remove(string key);
    void RemoveAll();
}
public static class Preferences
{
    public static readonly Dictionary<string, string> Values = [];
    public static void Remove(string key) => Values.Remove(key);
}
public sealed class TestSecureStorage : ISecureStorage
{
    public readonly Dictionary<string, string> Values = [];
    public bool FailWrites;
    public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));
    public Task SetAsync(string key, string value)
    {
        if (FailWrites) throw new IOException("Vault unavailable");
        Values[key] = value;
        return Task.CompletedTask;
    }
    public bool Remove(string key) => Values.Remove(key);
    public void RemoveAll() => Values.Clear();
}
