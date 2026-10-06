using SQLite;

namespace ACS_View.Domain.Entities
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public int PasswordHashVersion { get; set; } = 1;
        public string SecurityQuestion { get; set; } = string.Empty;
        public string SecurityAnswer { get; set; } = string.Empty;
        public string SecurityAnswerHash { get; set; } = string.Empty;
        public string SecurityAnswerSalt { get; set; } = string.Empty;
        public string RecoveryCodeHash { get; set; } = string.Empty;
        public string RecoveryCodeSalt { get; set; } = string.Empty;
        public int FailedLoginAttempts { get; set; }
        public long LoginBlockedUntilUtc { get; set; }
        public int FailedRecoveryAttempts { get; set; }
        public long RecoveryBlockedUntilUtc { get; set; }
        public int CredentialRevision { get; set; }
        public string BiometricTokenHash { get; set; } = string.Empty;
    }
}
