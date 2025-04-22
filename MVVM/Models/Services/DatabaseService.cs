using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _dataBasePath =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
                "health_app.db");

        public DatabaseService()
        {
            try
            {
                _database = new SQLiteAsyncConnection(_dataBasePath);
                Task.Run(async () => await InitializeDatabaseAsync()).Wait();
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            Console.WriteLine("Database initialization started...");
            try
            {
                // Fix: Split the CreateTablesAsync call into multiple calls with two type arguments each.  
                await _database.CreateTablesAsync<HealthRecord, Vaccines>();
                await _database.CreateTablesAsync<Note, House>();
                await _database.CreateTablesAsync<Family, User>();

                Console.WriteLine("Database initialization completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during database initialization: {ex.Message}\n");
                Console.WriteLine($"StackTrace: {ex.StackTrace}\n");
                throw new InvalidOperationException("Falha ao inicializar o banco de dados.", ex);
            }
        }

        public SQLiteAsyncConnection GetConnection()
        {
            try
            {
                if (_database == null)
                {
                    Task.Run(async () => await InitializeDatabaseAsync());
                    throw new InvalidOperationException("A conexão não foi inicializada. Certifique-se de chamar InitializeDatabaseAsync primeiro.");
                }
                return _database;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: ", ex.Message);
                throw;
            }
        }

        public Task<User> GetUserByUsernameAsync(string username)
        {
            return _database.Table<User>().FirstOrDefaultAsync(x => x.Username == username);
        }

        public Task<int> UpdateUserAsync(User user)
        {
            return _database.UpdateAsync(user);
        }
    }
}
