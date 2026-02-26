using ACS_View.Domain.Entities;
using SQLite;

namespace ACS_View.Domain.Interfaces
{
    internal interface IDatabaseService
    {
        Task InitializeAsync();
        SQLiteAsyncConnection Connection { get; }
        Task<User> GetUserByUsernameAsync(string username);
        Task<int> UpdateUserAsync(User user);
    }
}