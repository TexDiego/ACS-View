using ACS_View.Application.Interfaces;
using ACS_View.Application.Security;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using Microsoft.Maui.Storage;
using SQLite;
using System.Security.Cryptography;
using System.Text.Json;

internal static class WindowsAccountTests
{
    public static async Task<int> RunAsync()
    {
        var checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new Exception("FAIL Windows accounts: " + label);
            checks++; Console.WriteLine("PASS Windows accounts: " + label);
        }
        async Task Reject(Func<Task> action, string label)
        {
            try { await action(); }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or OperationCanceledException or JsonException)
            { Check(true, label); return; }
            throw new Exception("FAIL Windows accounts: unexpectedly accepted " + label);
        }
        var path = Path.Combine(Path.GetTempPath(), $"acs-hello-{Guid.NewGuid():N}.db");
        var connection = new SQLiteAsyncConnection(path);
        using var hello = new FakeWindowsHello();
        var vault = new FaultStorage();
        var clock = new WindowsTestClock();
        var context = new CurrentUserContext();
        var db = new DatabaseService(connection);
        var auth = new AuthService(db, context, vault, clock, new TestBiometricVault(), hello);
        var remembered = new RememberedAccountService(db, context, vault, clock, hello);
        const string password = "Senha segura para Windows Hello!";
        const string replacement = "Outra senha segura para Windows!";
        try
        {
            var recovery = await auth.RegisterAsync("helloAlice", password);
            await auth.RegisterAsync("helloBob", password);
            var alice = (await db.GetUserByUsernameAsync("helloAlice"))!;
            var bob = (await db.GetUserByUsernameAsync("helloBob"))!;
            Check((await remembered.GetAccountsAsync()).Count == 0, "existing users are never remembered automatically");
            await Reject(() => auth.LoginAsync("helloAlice", "wrong"), "failed password");
            await remembered.RememberCurrentAsync(true);
            Check((await remembered.GetAccountsAsync()).Count == 0, "unsuccessful login cannot remember an account");
            await auth.LoginAsync("helloAlice", password);
            await remembered.RememberCurrentAsync(false);
            Check((await remembered.GetAccountsAsync()).Count == 0, "first login without consent");
            await remembered.RememberCurrentAsync(true);
            Check(await remembered.IsRememberedAsync(alice.Id), "first authenticated opt-in");
            await remembered.RememberCurrentAsync(true);
            Check((await remembered.GetAccountsAsync()).Count == 1, "no duplicate profiles");
            clock.Advance();
            await auth.LoginAsync("helloBob", password);
            await remembered.RememberCurrentAsync(true);
            Check((await remembered.GetAccountsAsync()).Count == 2, "multiple remembered accounts");
            Check((await remembered.GetLastUsedAsync())!.UserId == bob.Id, "most recently used comes first");
            await auth.LoginAsync("helloAlice", password);
            clock.Advance();
            await remembered.RememberCurrentAsync(false);
            Check((await remembered.GetLastUsedAsync())!.UserId == alice.Id, "existing profile touch updates last access");
            var restarted = new RememberedAccountService(db, context, vault, clock, hello);
            Check((await restarted.GetAccountsAsync()).Count == 2, "remembered profiles survive service restart");
            Check((await auth.GetWindowsHelloStatusAsync()).Availability == WindowsHelloAvailability.Available, "available state");
            await Reject(() => auth.ConfigureWindowsHelloAsync("wrong", true), "enabling requires current password");
            Check(!(await auth.GetWindowsHelloStatusAsync()).Enabled, "invalid password cannot enable");
            hello.CancelCreate = true;
            await Reject(() => auth.ConfigureWindowsHelloAsync(password, true), "enrollment cancellation");
            hello.CancelCreate = false;
            Check(!(await auth.GetWindowsHelloStatusAsync()).Enabled, "cancel leaves no partial enrollment");
            vault.FailWrites = true;
            await Reject(() => auth.ConfigureWindowsHelloAsync(password, true), "enrollment storage failure");
            vault.FailWrites = false;
            Check(!(await auth.GetWindowsHelloStatusAsync()).Enabled && hello.Keys.Count == 0, "failed persistence deletes new native key");
            await auth.ConfigureWindowsHelloAsync(password, true);
            Check((await auth.GetWindowsHelloStatusAsync()).Enabled, "password and signed creation proof enable Hello");
            Check((await remembered.GetAccountsAsync()).Single(a => a.UserId == alice.Id).WindowsHelloEnabled, "selector reports account-specific Hello");
            Check(!vault.Values.Values.Any(v => v.Contains(password) || v.Contains(alice.PasswordHash) || v.Contains("PRIVATE KEY")), "no password, password hash, or private key stored in vault");
            await auth.LogoutAsync();
            Check(!context.HasCurrentUser && (await remembered.GetAccountsAsync()).Count == 2, "logout clears context and preserves profiles");
            Check((await auth.GetWindowsHelloStatusAsync(alice.Id)).Enabled, "logout preserves Windows enrollment");
            var enrollment = (await WindowsHelloEnrollment.ReadAsync(db, vault, alice.Id))!;
            var identity = await LocalDatabaseIdentity.GetAsync(db);
            var bobKey = WindowsHelloEnrollment.Key(identity, bob.Id);
            vault.Values[bobKey] = JsonSerializer.Serialize(enrollment);
            await Reject(() => auth.LoginWithWindowsHelloAsync(bob.Id), "Alice credential cannot authenticate Bob");
            Check(!context.HasCurrentUser, "cross-account rejection changes no context");
            vault.Values.Remove(bobKey);
            await auth.LoginWithWindowsHelloAsync(alice.Id);
            Check(context.CurrentUserId == alice.Id && await auth.IsAuthenticatedAsync(), "valid signed challenge creates normal session");
            var firstNonce = hello.LastChallenge!;
            await auth.LogoutAsync();
            await auth.LoginWithWindowsHelloAsync(alice.Id);
            Check(!firstNonce.SequenceEqual(hello.LastChallenge!), "each login has a fresh nonce");
            await auth.LogoutAsync();
            hello.ReplaySignature = hello.LastSignature;
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "reused signature fails fresh challenge");
            hello.ReplaySignature = null;
            hello.BadSignature = true;
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "signature failure");
            hello.BadSignature = false;
            hello.CancelSign = true;
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "authentication cancellation");
            hello.CancelSign = false;
            Check(!context.HasCurrentUser && (await db.GetUserByIdAsync(alice.Id))!.LoginBlockedUntilUtc == 0, "cancel neither authenticates nor blocks account");
            foreach (var state in new[] { WindowsHelloAvailability.NotConfiguredForUser, WindowsHelloAvailability.DeviceNotPresent, WindowsHelloAvailability.DisabledByPolicy, WindowsHelloAvailability.DeviceBusy, WindowsHelloAvailability.RetriesExhausted, WindowsHelloAvailability.Unavailable, WindowsHelloAvailability.PackageRequired })
            {
                hello.Availability = state;
                Check((await auth.GetWindowsHelloStatusAsync(alice.Id)).Availability == state, "availability " + state);
                await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "Hello rejects " + state);
                Check((await remembered.GetAccountsAsync()).Count == 2, "unavailability preserves profiles " + state);
            }
            await auth.LoginAsync("helloAlice", password);
            Check(context.CurrentUserId == alice.Id, "password still works without Hello");
            hello.Availability = WindowsHelloAvailability.Available;
            await auth.LogoutAsync();
            var originalKey = hello.Keys[enrollment.Name];
            hello.Keys[enrollment.Name] = RSA.Create(2048);
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "externally changed key");
            hello.Keys[enrollment.Name].Dispose(); hello.Keys[enrollment.Name] = originalKey;
            hello.Keys.Remove(enrollment.Name);
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "externally deleted key");
            hello.Keys[enrollment.Name] = originalKey;
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            hello.BeforeSign = async () => { entered.SetResult(); await release.Task; };
            var firstLogin = auth.LoginWithWindowsHelloAsync(alice.Id);
            await entered.Task;
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "concurrent Hello attempt rejected");
            await Reject(() => auth.LoginAsync("helloBob", password), "password cannot overlap native prompt");
            await Reject(() => remembered.ForgetAsync(alice.Id), "forget cannot race active authentication");
            release.SetResult(); await firstLogin; hello.BeforeSign = null;
            Check(context.CurrentUserId == alice.Id, "single successful concurrent authentication");
            await Reject(() => auth.ConfigureWindowsHelloAsync("wrong", false), "disable requires password");
            vault.FailRemoves = true;
            await Reject(() => auth.ConfigureWindowsHelloAsync(password, false), "revocation storage failure is reported");
            vault.FailRemoves = false;
            await auth.ConfigureWindowsHelloAsync(password, false);
            Check(!(await auth.GetWindowsHelloStatusAsync()).Enabled, "explicit disable revokes");
            await auth.LogoutAsync();
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "revoked enrollment rejected");
            await auth.LoginAsync("helloAlice", password);
            await auth.ConfigureWindowsHelloAsync(password, true);
            var oldEnrollment = (await WindowsHelloEnrollment.ReadAsync(db, vault, alice.Id))!;
            var nextRecovery = await auth.ConfigureRecoveryAsync(password, replacement);
            Check(!(await auth.GetWindowsHelloStatusAsync()).Enabled, "password rotation immediately invalidates Hello");
            Check(await remembered.IsRememberedAsync(alice.Id), "password rotation preserves remembered profile for fallback");
            await auth.LogoutAsync();
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "old revision cannot login");
            await auth.LoginAsync("helloAlice", replacement);
            await auth.ConfigureWindowsHelloAsync(replacement, true);
            await auth.ResetPasswordAsync("helloAlice", nextRecovery, password);
            Check(!(await auth.GetWindowsHelloStatusAsync(alice.Id)).Enabled, "recovery invalidates Hello");
            await auth.LoginAsync("helloAlice", password);
            await auth.ConfigureWindowsHelloAsync(password, true);
            await auth.LogoutAsync();
            for (var i = 0; i < 5; i++) await Reject(() => auth.LoginAsync("helloAlice", "wrong"), "failed attempt before lock " + i);
            var signCount = hello.SignCount;
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "account cooldown blocks Hello");
            Check(hello.SignCount == signCount && !context.HasCurrentUser, "blocked account never opens prompt");
            clock.Advance(TimeSpan.FromMinutes(6));
            await auth.LoginWithWindowsHelloAsync(alice.Id);
            Check(context.CurrentUserId == alice.Id, "Hello works after cooldown");
            await auth.LogoutAsync();
            var restorePath = path + ".restored";
            await connection.BackupAsync(restorePath);
            var restoredConnection = new SQLiteAsyncConnection(restorePath);
            try
            {
                var restoredDb = new DatabaseService(restoredConnection);
                var restoredAuth = new AuthService(restoredDb, context, vault, clock, new TestBiometricVault(), hello);
                var restoredAccounts = new RememberedAccountService(restoredDb, context, vault, clock, hello);
                Check((await restoredAccounts.GetAccountsAsync()).Count == 0, "copied DB cannot reuse profile IDs");
                await Reject(() => restoredAuth.LoginWithWindowsHelloAsync(alice.Id), "copied DB rejects enrollment");
            }
            finally { await restoredConnection.CloseAsync(); File.Delete(restorePath); }
            var stored = vault.Values["RememberedWindowsAccountsV1"];
            vault.Values["RememberedWindowsAccountsV1"] = "invalid json";
            Check((await remembered.GetAccountsAsync()).Count == 0, "corrupted selector fails safely");
            await auth.LoginAsync("helloAlice", password);
            await remembered.RememberCurrentAsync(true);
            Check((await remembered.GetAccountsAsync()).Count == 1, "explicit consent recovers corrupted selector");
            vault.Values["RememberedWindowsAccountsV1"] = stored;
            vault.FailReads = true;
            Check((await remembered.GetAccountsAsync()).Count == 0, "unavailable secure reads hide selector safely");
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "unavailable secure storage fails Hello closed");
            vault.FailReads = false;
            vault.FailWrites = true;
            await remembered.RememberCurrentAsync(true);
            vault.FailWrites = false;
            Check(context.CurrentUserId == alice.Id, "selector persistence failure preserves password session");
            hello.FailDelete = true;
            await remembered.ForgetAsync(alice.Id);
            hello.FailDelete = false;
            Check(!(await auth.GetWindowsHelloStatusAsync(alice.Id)).Enabled, "orphan native key has no local authorization after forget");
            Check(!await remembered.IsRememberedAsync(alice.Id) && await db.GetUserByIdAsync(alice.Id) is not null, "forget removes only profile, preserves real account");
            await auth.LogoutAsync();
            await Reject(() => auth.LoginWithWindowsHelloAsync(alice.Id), "forget revokes Hello");
            await connection.DeleteAsync(bob);
            Check((await remembered.GetAccountsAsync()).Count == 0, "deleted database user disappears from selector");
            await Reject(() => auth.LoginWithWindowsHelloAsync(bob.Id), "deleted user cannot authenticate");
            await auth.LoginAsync("helloAlice", password);
            Check(context.CurrentUserId == alice.Id, "traditional login remains usable after all failures");
            vault.FailWrites = vault.FailRemoves = true;
            await Reject(() => auth.LoginAsync("helloAlice", password), "session storage failure with removal failure");
            Check(!context.HasCurrentUser && !await auth.IsAuthenticatedAsync(), "vault removal failure cannot revive revoked in-memory session");
            vault.FailWrites = vault.FailRemoves = false;
            await auth.LoginAsync("helloAlice", password);
            var samePath = Path.Combine(Path.GetTempPath(), $"acs-same-path-{Guid.NewGuid():N}.db");
            var sameConnection = new SQLiteAsyncConnection(samePath);
            var sameDb = new DatabaseService(sameConnection);
            var beforeIdentity = await LocalDatabaseIdentity.GetAsync(sameDb);
            await sameConnection.BackupAsync(samePath + ".copy");
            await sameConnection.CloseAsync();
            File.Move(samePath, samePath + ".original");
            File.Move(samePath + ".copy", samePath);
            var afterConnection = new SQLiteAsyncConnection(samePath);
            try
            {
                var afterIdentity = await LocalDatabaseIdentity.GetAsync(new DatabaseService(afterConnection));
                Check(beforeIdentity != afterIdentity, "physical replacement at identical path rejects copied database GUID");
            }
            finally
            {
                await afterConnection.CloseAsync(); File.Delete(samePath); File.Delete(samePath + ".original");
            }
            return checks;
        }
        finally { await connection.CloseAsync(); File.Delete(path); }
    }
    private sealed class WindowsTestClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan? interval = null) => now += interval ?? TimeSpan.FromSeconds(1);
    }
    private sealed class FaultStorage : ISecureStorage
    {
        public readonly Dictionary<string, string> Values = [];
        public bool FailWrites, FailReads, FailRemoves;
        public Task<string?> GetAsync(string key) => FailReads ? throw new IOException() : Task.FromResult(Values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { if (FailWrites) throw new IOException(); Values[key] = value; return Task.CompletedTask; }
        public bool Remove(string key) => FailRemoves ? throw new IOException() : Values.Remove(key);
        public void RemoveAll() => Values.Clear();
    }
    private sealed class FakeWindowsHello : IWindowsHelloService, IDisposable
    {
        public readonly Dictionary<string, RSA> Keys = [];
        public WindowsHelloAvailability Availability = WindowsHelloAvailability.Available;
        public bool CancelCreate, CancelSign, BadSignature, FailDelete;
        public byte[]? LastChallenge, LastSignature, ReplaySignature;
        public int SignCount;
        public Func<Task>? BeforeSign;
        public Task<WindowsHelloAvailability> GetAvailabilityAsync() => Task.FromResult(Availability);
        public Task<byte[]> CreateAsync(string name)
        {
            if (CancelCreate) throw new OperationCanceledException();
            Keys.Add(name, RSA.Create(2048));
            return GetPublicKeyAsync(name);
        }
        public Task<byte[]> GetPublicKeyAsync(string name) => Keys.TryGetValue(name, out var key) ? Task.FromResult(key.ExportSubjectPublicKeyInfo()) : throw new InvalidOperationException("Key missing");
        public async Task<byte[]> SignAsync(string name, byte[] challenge)
        {
            SignCount++;
            if (BeforeSign is not null) await BeforeSign();
            if (CancelSign) throw new OperationCanceledException();
            LastChallenge = challenge.ToArray();
            if (ReplaySignature is not null) return ReplaySignature;
            LastSignature = BadSignature ? RandomNumberGenerator.GetBytes(256) : Keys[name].SignData(challenge, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return LastSignature;
        }
        public Task DeleteAsync(string name) { if (FailDelete) throw new IOException(); if (Keys.Remove(name, out var key)) key.Dispose(); return Task.CompletedTask; }
        public void Dispose() { foreach (var key in Keys.Values) key.Dispose(); }
    }
}
