using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class HouseService(DatabaseService databaseService)
    {
        private readonly SQLiteAsyncConnection _connection = databaseService.GetConnection();

        public Task<List<House>> GetAllHousesAsync() => _connection.Table<House>().OrderBy(h => h.CasaId).ToListAsync();

        public Task<House> GetHousesById(int id) => _connection.Table<House>().FirstOrDefaultAsync(h => h.CasaId == id);

        public async Task<int> SaveHouseAsync(House house)
        {
            return await _connection.InsertAsync(house);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _connection.Table<House>().CountAsync();
        }

        public async Task<int> GetMaxIdAsync()
        {
            try
            {
                // Obtém todos os registros e pega o maior valor de CasaId
                var maxId = await _connection.Table<House>()
                                             .OrderByDescending(h => h.CasaId)
                                             .Take(1)
                                             .ToListAsync();

                // Retorna o ID máximo ou 0 se não houver registros
                return maxId.FirstOrDefault()?.CasaId ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter o ID máximo: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateHouse(House house)
        {
            await _connection.UpdateAsync(house);
        }

        public async Task DeleteHouseAsync(int id)
        {
            try
            {
                var recordToDelete = await _connection.Table<House>().FirstOrDefaultAsync(r => r.CasaId == id);

                if (recordToDelete != null)
                {
                    await _connection.DeleteAsync(recordToDelete);
                    Console.WriteLine("Registro deletado do banco de dados.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar do banco de dados: {ex.Message}");
                throw;
            }
        }
    }
}
