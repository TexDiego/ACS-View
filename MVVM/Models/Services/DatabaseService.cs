using ACS_View.MVVM.Models.Interfaces;
using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class DatabaseService : IDatabaseService
    {
        private SQLiteAsyncConnection _database;
        private readonly string _databasePath;

        public DatabaseService()
        {
            _databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "health_app.db");

            MainThread.BeginInvokeOnMainThread(async () => await InitializeAsync());
        }

        public SQLiteAsyncConnection Connection
        {
            get
            {
                if (_database == null)
                    throw new InvalidOperationException("A conexão ainda não foi inicializada.");
                return _database;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                _database ??= new SQLiteAsyncConnection(_databasePath);

                await _database.CreateTablesAsync<HealthRecord, Vaccines>();
                await _database.CreateTablesAsync<Note, House>();
                await _database.CreateTablesAsync<Family, User>();
                await _database.CreateTableAsync<Visits>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar banco: {ex.Message}");
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