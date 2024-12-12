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

            InitializeDatabaseAsync();
        }

        public async Task InitializeDatabaseAsync()
        {
            await EnsureTableExistsAsync<HealthRecord>();
            await EnsureTableExistsAsync<Note>();
            await EnsureTableExistsAsync<House>();
            await EnsureTableExistsAsync<Family>();
        }

        private async Task EnsureTableExistsAsync<T>() where T : new()
        {
            var tableName = typeof(T).Name;

            // Verificar se a tabela existe
            var result = await _database.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'");

            if (result == 0)
            {
                // Criar tabela diretamente
                await _database.CreateTableAsync<T>();
            }
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
