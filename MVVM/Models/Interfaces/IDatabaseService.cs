using SQLite;

namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
        SQLiteAsyncConnection Connection { get; }
        Task<User> GetUserByUsernameAsync(string username);
        Task<int> UpdateUserAsync(User user);
    }
}