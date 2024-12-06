using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "health_app.db");
            _database = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeDatabaseAsync()
        {
            await _database.CreateTableAsync<HealthRecord>();
            await _database.CreateTableAsync<Note>();
        }

        public SQLiteAsyncConnection GetConnection()
        {
            if (_database == null)
            {
                throw new InvalidOperationException("A conexão não foi inicializada. Certifique-se de chamar InitializeDatabaseAsync primeiro.");
            }
            return _database;
        }
    }
}
