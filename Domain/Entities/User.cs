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
    }
}
