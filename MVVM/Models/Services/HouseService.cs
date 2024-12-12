using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class HouseService
    {
        private readonly SQLiteAsyncConnection _connection;

        public HouseService(DatabaseService databaseService)
        {
            _connection = databaseService.GetConnection();
        }

        public Task<List<House>> GetAllHousesAsync() => _connection.Table<House>().OrderBy(h => h.CasaId).ToListAsync();

        public Task<int> SaveHouseAsync(House house) => _connection.InsertAsync(house);

        public Task<int> DeleteHouseAsync(int id) => _connection.DeleteAsync<House>(id);
    }
}
