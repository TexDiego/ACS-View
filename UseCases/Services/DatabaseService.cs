using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Application.Security;
using ACS_View.Infrastructure.Data.Migrations;
using SQLite;
using System.Collections.Concurrent;

namespace ACS_View.UseCases.Services
{
    internal class DatabaseService(SQLiteAsyncConnection database) : IDatabaseService
    {
        private static readonly ConcurrentDictionary<string,SemaphoreSlim> InitializationLocks=new(
            OperatingSystem.IsWindows()?StringComparer.OrdinalIgnoreCase:StringComparer.Ordinal);
        private readonly SemaphoreSlim _initializeLock = InitializationLocks.GetOrAdd(Path.GetFullPath(database.DatabasePath),_=>new SemaphoreSlim(1,1));
        private bool _initialized;
        public SQLiteAsyncConnection Connection { get; } = database;
        public async Task InitializeAsync()
        {
            await _initializeLock.WaitAsync();
            try
            {
                if (_initialized) return;
                await Connection.ExecuteScalarAsync<int>("PRAGMA secure_delete = ON");
                await new SchemaMigrator(Connection).RunAsync();
                // Retain the existing credential upgrade, including secure removal of plaintext.
                // Its intentional security changes are separate from relational preservation.
                await MigrateUserTableAsync();
                _initialized = true;
            }
            finally { _initializeLock.Release(); }
        }
        public Task<User?> GetUserByUsernameAsync(string username) => Connection.Table<User>().FirstOrDefaultAsync(x => x.Username == username);
        public Task<User?> GetUserByIdAsync(int id) => Connection.Table<User>().FirstOrDefaultAsync(x => x.Id == id);
        public Task<int> InsertUserAsync(User user) => Connection.InsertAsync(user);
        public Task<int> UpdateUserAsync(User user) => Connection.UpdateAsync(user);
        public Task ClaimLegacyDataForUserAsync(int userId)
        {
            // Authentication is not evidence of ownership of unscoped legacy data.
            // Leave UserId=0 records quarantined until an explicit reconciliation workflow.
            return Task.CompletedTask;
        }
        private async Task MigrateUserTableAsync()
        {
            await EnsureColumnAsync("User", nameof(User.PasswordHash), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.PasswordSalt), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.PasswordHashVersion), "INTEGER NOT NULL DEFAULT 1");
            await EnsureColumnAsync("User", nameof(User.SecurityAnswerHash), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.SecurityAnswerSalt), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.RecoveryCodeHash), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.RecoveryCodeSalt), "TEXT NOT NULL DEFAULT ''");
            await EnsureColumnAsync("User", nameof(User.FailedLoginAttempts), "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumnAsync("User", nameof(User.LoginBlockedUntilUtc), "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumnAsync("User", nameof(User.FailedRecoveryAttempts), "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumnAsync("User", nameof(User.RecoveryBlockedUntilUtc), "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumnAsync("User", nameof(User.CredentialRevision), "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumnAsync("User", nameof(User.BiometricTokenHash), "TEXT NOT NULL DEFAULT ''");
            var removedLegacySecrets = false;
            foreach (var user in await Connection.Table<User>().ToListAsync())
            {
                if (string.IsNullOrEmpty(user.Password) && string.IsNullOrEmpty(user.SecurityQuestion)
                    && string.IsNullOrEmpty(user.SecurityAnswer) && string.IsNullOrEmpty(user.SecurityAnswerHash)
                    && string.IsNullOrEmpty(user.SecurityAnswerSalt)) continue;
                await Task.Run(() =>
                {
                    if (string.IsNullOrEmpty(user.PasswordHash) && !string.IsNullOrEmpty(user.Password))
                        AccountSecurity.SetPassword(user, user.Password);
                    user.Password = string.Empty;
                    AccountSecurity.ClearSecurityQuestion(user);
                });
                await Connection.UpdateAsync(user);
                removedLegacySecrets = true;
            }
            // Rebuild once after migration to remove old credential contents from free pages.
            if (removedLegacySecrets) await Connection.ExecuteAsync("VACUUM");
        }

        private async Task EnsureColumnAsync(string tableName, string columnName, string definition)
        {
            if (await ColumnExistsAsync(tableName, columnName))
            {
                return;
            }

            await Connection.ExecuteAsync($"ALTER TABLE [{tableName}] ADD COLUMN {columnName} {definition}");
        }

        private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            var columns = await Connection.QueryAsync<TableColumnInfo>($"PRAGMA table_info([{tableName}])");
            return columns.Any(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));
        }

        private sealed class TableColumnInfo
        {
            [Column("name")] public string Name { get; set; } = string.Empty;
        }
    }
}
