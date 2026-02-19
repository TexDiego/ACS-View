using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    internal class FamilyService : IFamilyService
    {
        private readonly IDatabaseService _databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();
        private readonly SQLiteAsyncConnection _connection;

        public FamilyService()
        {
            _connection = _databaseService.Connection;
        }

        public async Task<List<Family>> GetAllFamiliesAsync()
        {
            try
            {
                return await _connection.QueryAsync<Family>("SELECT * FROM Family ORDER BY IdFamilia");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar famílias: {ex.Message}");
                return [];
            }
        }

        public async Task<Family> GetFamilyById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID deve ser maior que zero.", nameof(id));

            try
            {
                var result = await _connection.QueryAsync<Family>("SELECT * FROM Family WHERE IdFamilia = ?", id);
                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar família por ID: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Patient>> GetPersonOfFamilyById(int houseId, int familyId)
        {
            if (houseId <= 0)
                throw new ArgumentException("O ID da residência deve ser maior que zero.", nameof(houseId));

            try
            {
                return await _connection.QueryAsync<Patient>(
                    "SELECT * FROM HealthRecord WHERE HouseId = ? AND FamilyId = ?",
                    houseId, familyId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar pessoas da família: {ex.Message}");
                return [];
            }
        }

        public async Task<int> SaveFamilyAsync(Family family)
        {
            if (family == null)
                throw new ArgumentNullException(nameof(family), "O objeto Família não pode ser nulo.");

            try
            {
                return await _connection.ExecuteAsync(
                    "INSERT INTO Family (IdFamilia, IdPessoa) VALUES (?, ?)",
                    family.IdFamilia, family.IdPessoa
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar família: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteFamilyAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID deve ser maior que zero.", nameof(id));

            try
            {
                return await _connection.ExecuteAsync("DELETE FROM Family WHERE IdFamilia = ?", id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar família: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateFamilyAsync(Family family)
        {
            if (family == null)
                throw new ArgumentNullException(nameof(family), "O objeto Família não pode ser nulo.");

            try
            {
                await _connection.ExecuteAsync(
                    "UPDATE Family SET IdPessoa = ? WHERE IdFamilia = ?",
                    family.IdPessoa, family.IdFamilia
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar família: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetMaxIdAsync(int houseId)
        {
            if (houseId <= 0)
                throw new ArgumentException("O ID da residência deve ser maior que zero.", nameof(houseId));

            try
            {
                var result = await _connection.ExecuteScalarAsync<int?>(
                    "SELECT MAX(FamilyId) FROM Patient WHERE HouseId = ?", houseId
                );
                return result ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter o maior IdFamilia para HouseId={houseId}: {ex.Message}");
                throw;
            }
        }
    }
}