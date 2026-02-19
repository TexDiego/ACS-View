using ACS_View.MVVM.Models.HealthConditions;
using ACS_View.MVVM.Models.Interfaces;
using SQLite;
using System.Diagnostics;

namespace ACS_View.MVVM.Models.Services
{
    public class DatabaseService : IDatabaseService
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
                await _database.CreateTablesAsync<ConditionCategory, DashboardItem>();
                await _database.CreateTablesAsync<PatientCondition, HealthConditions.Condition>();
                await _database.CreateTablesAsync<Note, House>();
                await _database.CreateTablesAsync<Family, User>();
                await _database.CreateTableAsync<Visits>();

                if (await _database.Table<ConditionCategory>().CountAsync() <= 0)
                    await SeedAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao inicializar banco: {ex.Message}");
                throw;
            }
        }

        public async Task SeedAsync()
        {
            var categoryMap = new Dictionary<string, int>();

            foreach (var item in ConditionCatalog.All)
            {
                if (!categoryMap.TryGetValue(item.Category, out int value))
                {
                    var category = new ConditionCategory { Name = item.Category };
                    await _database.InsertAsync(category);
                    value = category.Id;
                    categoryMap[item.Category] = value;
                }

                var condition = new HealthConditions.Condition
                {
                    Name = item.Condition,
                    CategoryId = value
                };

                await _database.InsertAsync(condition);
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