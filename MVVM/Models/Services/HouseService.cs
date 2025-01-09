using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class HouseService(DatabaseService databaseService)
    {
        private readonly SQLiteAsyncConnection _connection = databaseService.GetConnection();

        public async Task<List<House>> GetAllHousesAsync()
        {
            try
            {
                // Consulta SQL direta para buscar todas as casas
                return await _connection.QueryAsync<House>("SELECT * FROM House");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar casas: {ex.Message}");
                return new List<House>();
            }
        }

        public async Task<House> GetHousesById(int id)
        {
            try
            {
                // Consulta SQL direta para buscar uma casa pelo ID
                var result = await _connection.QueryAsync<House>("SELECT * FROM House WHERE CasaId = ?", id);
                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar casa por ID: {ex.Message}");
                throw;
            }
        }

        public async Task<House> GetHouseBySusAsync(string susNumber)
        {
            try
            {
                var query = @"
                            SELECT h.*
                            FROM House h
                            JOIN HealthRecord r ON h.CasaId = r.HouseId
                            WHERE r.SusNumber = ?";

                // Retorna o objeto House completo
                var result = await _connection.FindWithQueryAsync<House>(query, susNumber);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar casa pelo SUS: {ex.Message}");
                throw new Exception("Erro ao buscar informações da casa.");
            }
        }

        public async Task<int> SaveHouseAsync(House house)
        {
            try
            {
                // Inserção direta no banco de dados
                return await _connection.ExecuteAsync(
                    "INSERT INTO House (CasaId, CEP, Rua, NumeroCasa, Bairro, Cidade, Estado, Pais, Complemento, PossuiComplemento) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    house.CasaId,
                    house.CEP,
                    house.Rua,
                    house.NumeroCasa,
                    house.Bairro,
                    house.Cidade,
                    house.Estado,
                    house.Pais,
                    house.Complemento,
                    house.PossuiComplemento
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar casa: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetTotalCountAsync()
        {
            try
            {
                // Consulta SQL para contar o total de registros
                var result = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM House");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao contar casas: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetMaxIdAsync()
        {
            try
            {
                // Consulta SQL para obter o ID máximo
                var result = await _connection.ExecuteScalarAsync<int?>("SELECT MAX(CasaId) FROM House");
                return result ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter o ID máximo: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateHouseAsync(House house)
        {
            try
            {
                // Atualização direta com consulta SQL
                await _connection.ExecuteAsync(
                    "UPDATE House SET CEP = ?, Rua = ?, NumeroCasa = ?, Bairro = ?, Cidade = ?, Estado = ?, Pais = ?, Complemento = ?, PossuiComplemento = ? " +
                    "WHERE CasaId = ?",
                    house.CEP,
                    house.Rua,
                    house.NumeroCasa,
                    house.Bairro,
                    house.Cidade,
                    house.Estado,
                    house.Pais,
                    house.Complemento,
                    house.PossuiComplemento,
                    house.CasaId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar casa: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteHouseAsync(int id)
        {
            try
            {
                // Exclusão direta pelo ID
                await _connection.ExecuteAsync("DELETE FROM House WHERE CasaId = ?", id);
                Console.WriteLine("Registro deletado do banco de dados.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar do banco de dados: {ex.Message}");
                throw;
            }
        }
    }
}
