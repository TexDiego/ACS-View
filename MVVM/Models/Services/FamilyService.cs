using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class FamilyService
    {
        private readonly SQLiteAsyncConnection _connection;

        public FamilyService(DatabaseService databaseService)
        {
            _connection = databaseService.GetConnection();
        }

        public Task<List<Family>> GetAllFamiliesAsync() => _connection.Table<Family>().OrderBy(f => f.IdFamilia).ToListAsync();

        public Task<int> SaveFamilyAsync(Family family) => _connection.InsertAsync(family);

        public Task<int> DeleteFamilyAsync(int id) => _connection.DeleteAsync<Family>(id);
    }
}
