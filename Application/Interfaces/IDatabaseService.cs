using ACS_View.Domain.Entities;
using SQLite;

namespace ACS_View.Application.Interfaces
{
    public interface IDatabaseService
    {
        Task InitializeAsync();
        SQLiteAsyncConnection Connection { get; }
        Task<User?> GetUserByUsernameAsync(string username);
        Task<int> InsertUserAsync(User user);
        Task<int> UpdateUserAsync(User user);
    }
}
