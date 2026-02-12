using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using SQLite;
using System.Diagnostics;

namespace ACS_View.MVVM.Models.Services
{
    public class HouseService : IHouseService
    {
        private readonly IDatabaseService _databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();
        private readonly SQLiteAsyncConnection _connection;

        public HouseService()
        {
            _connection = _databaseService.Connection;
        }

        public async Task DeleteHouseAsync(int id)
        {
            try
            {
                var house = await GetHouseByIdAsync(id);
                if (house != null)
                {
                    await _connection.DeleteAsync(house);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao deletar do banco de dados: {ex.Message}");
                throw;
            }
        }

        public async Task<List<House>> GetAllHousesAsync()
        {
            try
            {
                return await _connection.Table<House>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casas: {ex.Message}");
                return [];
            }
        }

        public async Task<House?> GetHouseByIdAsync(int id)
        {
            try
            {
                return await _connection.FindAsync<House>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casa por ID: {ex.Message}");
                throw;
            }
        }

        public async Task<House?> GetHouseBySusAsync(string susNumber)
        {
            try
            {
                var query = @"
                    SELECT h.*
                    FROM House h
                    JOIN HealthRecord r ON h.CasaId = r.HouseId
                    WHERE r.SusNumber = ?";

                var result = await _connection.FindWithQueryAsync<House>(query, susNumber);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casa pelo SUS: {ex.Message}");
                throw new Exception("Erro ao buscar informações da casa.");
            }
        }

        public async Task<int> GetMaxIdAsync()
        {
            try
            {
                var maxId = await _connection.ExecuteScalarAsync<int?>("SELECT MAX(CasaId) FROM House");
                return maxId ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter o ID máximo: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetTotalCountAsync()
        {
            try
            {
                var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM House");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao contar casas: {ex.Message}");
                throw;
            }
        }

        public async Task SaveHouseAsync(House house)
        {
            try
            {
                await _connection.InsertAsync(house);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar casa: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateHouseAsync(House house)
        {
            try
            {
                await _connection.UpdateAsync(house);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar casa: {ex.Message}");
                throw;
            }
        }
    }
}