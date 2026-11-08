global using Microsoft.Maui.Storage;
using ACS_View.Application.Interfaces;

namespace Microsoft.Maui.Storage
{
    public interface ISecureStorage
    {
        Task<string?> GetAsync(string key); Task SetAsync(string key, string value); bool Remove(string key); void RemoveAll();
    }
    public sealed class MemoryStorage : ISecureStorage
    {
        private readonly Dictionary<string, string> values = [];
        public Task<string?> GetAsync(string key) => Task.FromResult(values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { values[key] = value; return Task.CompletedTask; }
        public bool Remove(string key) => values.Remove(key);
        public void RemoveAll() => values.Clear();
    }
    public static class SecureStorage { public static ISecureStorage Default { get; } = new MemoryStorage(); }
    public static class Preferences
    {
        private static readonly Dictionary<string, string> values = [];
        public static string Get(string key, string fallback) => values.GetValueOrDefault(key, fallback);
        public static void Set(string key, string value) => values[key] = value;
        public static void Remove(string key) => values.Remove(key);
    }
    public sealed class AppInfo
    {
        public static AppInfo Current { get; } = new(); public string VersionString => "test";
    }
}
internal sealed class NoBiometric : ILocalBiometricVault
{
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);
    public Task<bool> HasEnrollmentAsync() => Task.FromResult(false);
    public Task ProtectAsync(string payload) => throw new NotSupportedException();
    public Task<string> UnlockAsync() => throw new NotSupportedException();
    public Task RemoveAsync() => Task.CompletedTask;
}
