using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class FamilyService
    {
        private readonly SQLiteAsyncConnection _connection;

        public FamilyService(DatabaseService databaseService)
        {
            _connection = databaseService?.GetConnection()
                          ?? throw new ArgumentNullException(nameof(databaseService));
        }

        public async Task<List<Family>> GetAllFamiliesAsync()
        {
            try
            {
                // Consulta SQL direta para obter todas as famílias, ordenadas por IdFamilia
                return await _connection.QueryAsync<Family>("SELECT * FROM Family ORDER BY IdFamilia");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar famílias: {ex.Message}");
                return new List<Family>();
            }
        }

        public async Task<Family> GetFamilyById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID deve ser maior que zero.", nameof(id));

            try
            {
                // Consulta SQL direta para buscar uma família pelo ID
                var result = await _connection.QueryAsync<Family>("SELECT * FROM Family WHERE IdFamilia = ?", id);
                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar família por ID: {ex.Message}");
                throw;
            }
        }

        public async Task<List<HealthRecord>> GetPersonOfFamilyById(int houseId, int familyId)
        {
            if (houseId <= 0)
                throw new ArgumentException("O ID da residência deve ser maior que zero.", nameof(houseId));

            try
            {
                // Consulta SQL direta para buscar os registros de saúde associados a uma família específica
                return await _connection.QueryAsync<HealthRecord>(
                    "SELECT * FROM HealthRecord WHERE HouseId = ? AND FamilyId = ?",
                    houseId, familyId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar pessoas da família: {ex.Message}");
                return new List<HealthRecord>();
            }
        }

        public async Task<int> SaveFamilyAsync(Family family)
        {
            if (family == null)
                throw new ArgumentNullException(nameof(family), "O objeto Família não pode ser nulo.");

            try
            {
                // Inserção direta no banco de dados
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
                // Exclusão direta de uma família pelo ID
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
                // Atualização direta no banco de dados
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
                // Consulta SQL para obter o maior IdFamilia associado a um HouseId específico
                var result = await _connection.ExecuteScalarAsync<int?>(
                    "SELECT MAX(FamilyId) FROM HealthRecord WHERE HouseId = ?", houseId
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
