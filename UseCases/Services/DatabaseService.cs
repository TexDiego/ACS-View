using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Interfaces;
using SQLite;
using System.Diagnostics;

namespace ACS_View.UseCases.Services
{
    internal class DatabaseService : IDatabaseService
    {
        private SQLiteAsyncConnection _database;
        private readonly string _databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "health_app.db");

        public DatabaseService()
        {
            MainThread.BeginInvokeOnMainThread(async () => await InitializeAsync());
        }

        public SQLiteAsyncConnection Connection
        {
            get
            {
                if (_database == null) throw new InvalidOperationException("A conexão ainda não foi inicializada.");
                return _database;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                _database ??= new SQLiteAsyncConnection(_databasePath);

                await _database.CreateTablesAsync<Patient, Vaccines>();
                await _database.CreateTablesAsync<Note, House>();
                await _database.CreateTablesAsync<Family, User>();
                await _database.CreateTablesAsync<CidCategory, CidChapter>();
                await _database.CreateTablesAsync<CidGroup, CidSubcategory>();
                await _database.CreateTableAsync<Visits>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao inicializar banco: {ex.Message}");
                throw;
            }
        }

        public Task<User> GetUserByUsernameAsync(string username)
        {
            return Connection.Table<User>().FirstOrDefaultAsync(x => x.Username == username);
        }

        public Task<int> UpdateUserAsync(User user)
        {
            return Connection.UpdateAsync(user);
        }
    }
}