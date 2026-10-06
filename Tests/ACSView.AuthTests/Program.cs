using ACS_View.Application.Security;
using ACS_View.Domain.Entities;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using Microsoft.Maui.Storage;
using SQLite;
using System.Security.Cryptography;

SQLitePCL.Batteries_V2.Init();
var checks = 0;
const string password = "Minha frase de senha local!";
const string replacement = "Outra frase segura para entrar!";
var testPath = Path.Combine(Path.GetTempPath(), $"acs-auth-{Guid.NewGuid():N}.db");
var connection = new SQLiteAsyncConnection(testPath);
try
{
    // Start with the exact old schema, rather than a new User table.
    await connection.ExecuteAsync("CREATE TABLE User (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT, Password TEXT, SecurityQuestion TEXT, SecurityAnswer TEXT)");
    await connection.ExecuteAsync("INSERT INTO User (Username, Password, SecurityQuestion, SecurityAnswer) VALUES (?, ?, ?, ?)", "antigo", "senha antiga", "Animal?", "gato");
    var legacyReminderTime = DateTime.Now.AddDays(3);
    await connection.ExecuteAsync("CREATE TABLE Note (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, Content TEXT, CreationDate BIGINT, NotifyOn BIGINT)");
    await connection.ExecuteAsync("INSERT INTO Note (UserId, Content, CreationDate, NotifyOn) VALUES (?, ?, ?, ?)", 1, "Nota de teste anterior à atualização", DateTime.Now, legacyReminderTime);
    var db = new DatabaseService(connection);
    await db.InitializeAsync();
    Check((await connection.GetTableInfoAsync("Note")).Any(c => c.Name == "ReminderMessage"), "Note schema includes recoverable reminder message");
    var preservedReminder = await connection.FindAsync<Note>(1);
    Check(preservedReminder.NotifyOn == legacyReminderTime && preservedReminder.Content == "Nota de teste anterior à atualização", "Additive reminder migration preserves existing SQLite note");
    var reminderStore = new SQLiteNoteReminderStore(db);
    var storedReminder = (await reminderStore.GetAllAsync()).Single(n => n.Id == 1);
    await reminderStore.SetAsync(storedReminder, legacyReminderTime.AddHours(1), "Mensagem de teste");
    preservedReminder = await connection.FindAsync<Note>(1);
    Check(preservedReminder.NotifyOn == legacyReminderTime.AddHours(1) && preservedReminder.ReminderMessage == "Mensagem de teste" && preservedReminder.UserId == 1 && preservedReminder.Content == storedReminder.Content, "Reminder persistence changes only reminder fields with owner guard");
    var old = (await db.GetUserByUsernameAsync("antigo"))!;
    Check(old.Password == "" && old.SecurityAnswer == "" && old.SecurityQuestion == "", "Migration clears plaintext and questions");
    Check(PasswordHasher.Verify("senha antiga", old.PasswordHash, old.PasswordSalt, old.PasswordHashVersion), "Migration preserves old password");
    Check((await connection.GetTableInfoAsync("User")).Any(c => c.Name == "RecoveryCodeHash"), "Migration adds recovery fields to old table");
    Check(await connection.ExecuteScalarAsync<int>("PRAGMA secure_delete") == 1, "Secure deletion enabled for SQLite migration");
    await new DatabaseService(connection).InitializeAsync();
    Check((await db.GetUserByUsernameAsync("antigo"))!.PasswordHash == old.PasswordHash, "Repeated migration preserves credential hash and identity");

    var vault = new TestSecureStorage();
    var clock = new TestClock();
    var context = new CurrentUserContext();
    var biometrics = new TestBiometricVault();
    var auth = new AuthService(db, context, vault, clock, biometrics);
    await Reject(() => auth.RegisterAsync("curta", "abc"), "Short new password rejected");
    await Reject(() => auth.RegisterAsync("ab", password), "Short username rejected");
    var code = await auth.RegisterAsync("alice", password);
    var alice = (await db.GetUserByUsernameAsync("alice"))!;
    var note = new Note { UserId = alice.Id, Content = "Registro que deve ser preservado" };
    await connection.InsertAsync(note);
    Check(code.Length == 39 && alice.Password == "" && alice.PasswordHash != password && alice.RecoveryCodeHash != code, "No plaintext credentials saved");
    Check(alice.PasswordHashVersion == 2 && alice.SecurityAnswerHash == "", "Versioned password and no question");
    await Reject(() => auth.RegisterAsync("alice", password), "Duplicate username rejected");
    await Reject(() => auth.LoginAsync("alice", ""), "Empty-password regression rejected");
    Check(!context.HasCurrentUser, "Failed login creates no context");
    await auth.LoginAsync("alice", password);
    Check(context.CurrentUserId == alice.Id && await auth.IsAuthenticatedAsync(), "Correct password creates restorable protected session");
    Check(!vault.Values.Values.Any(v => v.Contains(password) || v.Contains(code)), "Vault stores no password or recovery code");

    var staleSession = vault.Values["LocalAuthSessionV2"];
    await Reject(() => auth.ResetPasswordAsync("alice", "gato", replacement), "Old answer cannot recover account");
    var nextCode = await auth.ResetPasswordAsync("alice", code.ToLowerInvariant().Replace("-", " "), replacement);
    Check(nextCode != code && !context.HasCurrentUser && !await auth.IsAuthenticatedAsync(), "Recovery rotates code and logs out");
    Check((await connection.FindAsync<Note>(note.Id)).Content == note.Content && (await connection.FindAsync<Note>(note.Id)).UserId == alice.Id, "Recovery preserves user records and ownership");
    await Reject(() => auth.ResetPasswordAsync("alice", code, password), "Used code rejected");
    await Reject(() => auth.LoginAsync("alice", password), "Old password rejected after recovery");
    vault.Values["LocalAuthSessionV2"] = staleSession;
    Check(!await auth.IsAuthenticatedAsync(), "Old session revision rejected");
    await auth.LoginAsync("alice", replacement);
    await Reject(() => auth.ConfigureRecoveryAsync("wrong", password), "Security changes require actual current password");
    var configuredCode = await auth.ConfigureRecoveryAsync(replacement, password);
    Check(configuredCode != nextCode && await auth.IsAuthenticatedAsync(), "Authenticated rotation renews code and session");
    await Reject(() => auth.ResetPasswordAsync("alice", nextCode, replacement), "Replaced recovery code rejected");

    await auth.LogoutAsync();
    await auth.LoginAsync("antigo", "senha antiga");
    var migrationCode = await auth.ConfigureRecoveryAsync("senha antiga", password);
    Check(migrationCode.Length == 39 && (await db.GetUserByIdAsync(old.Id))!.Id == old.Id, "Old account can adopt recovery without changing identity");
    await auth.LogoutAsync();

    // Old PBKDF2 100k hash is verified with its original cost, then upgraded.
    var salt = RandomNumberGenerator.GetBytes(16);
    var v1 = new User { Username = "version1", PasswordHashVersion = 1, PasswordSalt = Convert.ToBase64String(salt),
        PasswordHash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32)) };
    await db.InsertUserAsync(v1);
    await auth.LoginAsync("version1", password);
    Check((await db.GetUserByIdAsync(v1.Id))!.PasswordHashVersion == 2, "Old hash upgraded after successful login");
    await auth.LogoutAsync();

    await auth.RegisterAsync("limit", password);
    for (var i = 0; i < 5; i++) await Reject(() => auth.LoginAsync("limit", "wrong"), $"Login failure {i + 1}");
    // Recreate services and SQLite connection to prove the cooldown survives restart.
    var restartedConnection = new SQLiteAsyncConnection(testPath);
    var restarted = new AuthService(new DatabaseService(restartedConnection), context, vault, clock, biometrics);
    await Reject(() => restarted.LoginAsync("limit", password), "Persistent login cooldown blocks correct password");
    clock.Advance(TimeSpan.FromMinutes(5));
    await restarted.LoginAsync("limit", password);
    Check(context.HasCurrentUser, "Login allowed after cooldown");
    await restarted.LogoutAsync();
    for (var i = 0; i < 5; i++) await Reject(() => restarted.ResetPasswordAsync("limit", new string('A', 32), password), $"Recovery failure {i + 1}");
    await Reject(() => auth.ResetPasswordAsync("limit", new string('A', 32), password), "Persistent recovery cooldown shared across service instances");
    Check((await db.GetUserByUsernameAsync("limit"))!.RecoveryBlockedUntilUtc > clock.GetUtcNow().ToUnixTimeSeconds(), "Recovery cooldown persisted");
    await restartedConnection.CloseAsync();

    await auth.LoginAsync("alice", password);
    clock.Advance(TimeSpan.FromDays(8));
    Check(!await auth.IsAuthenticatedAsync(), "Session expires after seven days");
    Preferences.Values["AuthToken"] = "forged";
    Preferences.Values["AuthUserId"] = alice.Id.ToString();
    Check(!await auth.IsAuthenticatedAsync() && Preferences.Values.Count == 0, "Legacy preferences cannot authenticate");
    vault.Values["LocalAuthSessionV2"] = "invalid json";
    Check(!await auth.IsAuthenticatedAsync(), "Malformed stored session fails closed");
    vault.FailWrites = true;
    await Reject(() => auth.LoginAsync("alice", password), "Vault failure rejects login");
    Check(!context.HasCurrentUser, "Vault failure leaves no authenticated context");
    Check(!PasswordHasher.Verify(password, "bad base64", "bad base64"), "Corrupted hash fails closed");
    Check(!PasswordHasher.Verify(password, alice.PasswordHash, alice.PasswordSalt, 999), "Unknown hash version fails closed");
    vault.FailWrites = false;
    await auth.LoginAsync("alice", password);
    vault.FailWrites = true;
    var failureCode = await auth.ConfigureRecoveryAsync(password, replacement);
    Check(failureCode.Length == 39 && !context.HasCurrentUser, "Vault renewal failure preserves recovery code and fails session closed");
    vault.FailWrites = false;
    await auth.LoginAsync("alice", replacement);
    var raceCode = await auth.ConfigureRecoveryAsync(replacement, password);
    var resets = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
    {
        try { await auth.ResetPasswordAsync("alice", raceCode, replacement); return true; }
        catch (InvalidOperationException) { return false; }
    }));
    Check(resets.Count(success => success) == 1, "Concurrent requests cannot reuse the same recovery code");
    await Reject(() => auth.ConfigureBiometricsAsync(replacement, true), "Biometric enrollment requires signed-in account");
    await auth.LoginAsync("alice", replacement);
    await Reject(() => auth.ConfigureBiometricsAsync("wrong", true), "Biometric enrollment requires correct current password");
    biometrics.Available = false;
    await Reject(() => auth.ConfigureBiometricsAsync(replacement, true), "Unavailable biometrics cannot be enabled");
    biometrics.Available = true;
    biometrics.CancelEnrollment = true;
    try { await auth.ConfigureBiometricsAsync(replacement, true); throw new Exception("Cancellation accepted"); }
    catch (OperationCanceledException) { Check(!(await auth.GetBiometricStatusAsync()).Enabled, "Cancelled enrollment does not activate biometrics"); }
    biometrics.CancelEnrollment = false;
    await auth.ConfigureBiometricsAsync(replacement, true);
    var grant = biometrics.Payload!;
    Check((await auth.GetBiometricStatusAsync()).Enabled && !grant.Contains(replacement), "Biometrics enrolls a random grant without storing password");
    Check(!string.IsNullOrEmpty((await db.GetUserByIdAsync(alice.Id))!.BiometricTokenHash), "Database stores only hash of biometric token");
    Check(!await auth.IsAuthenticatedAsync() && !context.HasCurrentUser, "Persisted session cannot bypass biometric lock on startup or resume");
    biometrics.Available = false;
    Check(!await auth.IsAuthenticatedAsync(), "Unavailable sensor never permits automatic session restoration");
    biometrics.Available = true;
    biometrics.CancelUnlock = true;
    try { await auth.LoginWithBiometricsAsync(); throw new Exception("Cancellation accepted"); }
    catch (OperationCanceledException) { Check(!context.HasCurrentUser, "Cancelled biometric prompt leaves session locked"); }
    biometrics.CancelUnlock = false;
    await auth.LoginWithBiometricsAsync();
    Check(context.CurrentUserId == alice.Id, "Successful biometric proof unlocks enrolled account");
    Check((await connection.FindAsync<Note>(note.Id)).Content == note.Content, "Biometric login preserves user data");
    await Reject(() => auth.ConfigureBiometricsAsync("wrong", false), "Disabling biometrics also requires current password");
    await auth.ConfigureBiometricsAsync(replacement, false);
    Check(!(await auth.GetBiometricStatusAsync()).Enabled && biometrics.Payload is null, "Explicit disable removes protected grant");
    await auth.ConfigureBiometricsAsync(replacement, true);
    var enrolledGrant = biometrics.Payload!;
    context.Clear();
    var modified = System.Text.Json.Nodes.JsonNode.Parse(enrolledGrant)!;
    modified["Token"] = Convert.ToBase64String(new byte[32]);
    biometrics.Payload = modified.ToJsonString();
    await Reject(() => auth.LoginWithBiometricsAsync(), "Forged biometric token cannot authenticate");
    Check(!context.HasCurrentUser, "Forged token leaves no user context");
    await auth.LoginAsync("alice", replacement);
    await auth.ConfigureBiometricsAsync(replacement, true);
    var beforePasswordChange = biometrics.Payload!;
    await auth.ConfigureRecoveryAsync(replacement, password);
    Check(!(await auth.GetBiometricStatusAsync()).Enabled && biometrics.Payload is null, "Password change revokes biometric enrollment");
    biometrics.Payload = beforePasswordChange;
    vault.Values["BiometricAccountV1"] = alice.Id.ToString();
    await Reject(() => auth.LoginWithBiometricsAsync(), "Stale grant cannot authenticate after password change");
    await auth.LoginAsync("alice", password);
    await auth.ConfigureBiometricsAsync(password, true);
    var recoveryCodeForBio = await auth.ConfigureRecoveryAsync(password, password);
    await auth.ConfigureBiometricsAsync(password, true);
    await auth.ResetPasswordAsync("alice", recoveryCodeForBio, replacement);
    Check(!(await auth.GetBiometricStatusAsync()).Enabled && biometrics.Payload is null, "Recovery revokes biometric enrollment");
    await auth.LoginAsync("alice", replacement);
    await auth.ConfigureBiometricsAsync(replacement, true);
    await auth.LogoutAsync();
    Check(!(await auth.GetBiometricStatusAsync()).Enabled && !context.HasCurrentUser, "Explicit logout revokes biometrics and password session");
    await auth.LoginAsync("alice", replacement);
    await auth.ConfigureBiometricsAsync(replacement, true);
    await auth.LoginAsync("antigo", password);
    Check(!(await auth.GetBiometricStatusAsync()).Enabled && biometrics.Payload is null, "Switching accounts removes previous biometric enrollment");
    Console.WriteLine($"PASS: {checks} authentication, biometric and migration checks.");
}
finally
{
    await connection.CloseAsync();
    File.Delete(testPath);
}

void Check(bool condition, string label)
{
    if (!condition) throw new Exception($"FAIL: {label}");
    checks++;
    Console.WriteLine($"PASS: {label}");
}
async Task Reject(Func<Task> action, string label)
{
    try { await action(); }
    catch (InvalidOperationException) { Check(true, label); return; }
    throw new Exception($"FAIL: {label} unexpectedly succeeded");
}
sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.Parse("2026-10-06T12:00:00Z");
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan interval) => now += interval;
}
