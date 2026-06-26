using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class FamilyService(IDatabaseService db, ICurrentUserContext currentUserContext) : IFamilyService
    {
        private readonly SQLiteAsyncConnection _connection = db.Connection;

        public async Task<List<Family>> GetAllFamiliesAsync()
        {
            try
            {
                var userId = currentUserContext.RequireCurrentUserId();
                return await _connection.QueryAsync<Family>("SELECT * FROM Family WHERE UserId = ? ORDER BY IdFamilia", userId);
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
                var userId = currentUserContext.RequireCurrentUserId();
                var result = await _connection.QueryAsync<Family>("SELECT * FROM Family WHERE IdFamilia = ? AND UserId = ?", id, userId);
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
                    "SELECT * FROM Patient WHERE HouseId = ? AND FamilyId = ? AND UserId = ?",
                    houseId, familyId, currentUserContext.RequireCurrentUserId()
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
                family.UserId = currentUserContext.RequireCurrentUserId();
                return await _connection.ExecuteAsync(
                    "INSERT INTO Family (IdFamilia, UserId, IdPessoa) VALUES (?, ?, ?)",
                    family.IdFamilia, family.UserId, family.IdPessoa
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
                var userId = currentUserContext.RequireCurrentUserId();
                return await _connection.ExecuteAsync("DELETE FROM Family WHERE IdFamilia = ? AND UserId = ?", id, userId);
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
                family.UserId = currentUserContext.RequireCurrentUserId();
                await _connection.ExecuteAsync(
                    "UPDATE Family SET IdPessoa = ? WHERE IdFamilia = ? AND UserId = ?",
                    family.IdPessoa, family.IdFamilia, family.UserId
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
                    "SELECT MAX(FamilyId) FROM Patient WHERE HouseId = ? AND UserId = ?",
                    houseId,
                    currentUserContext.RequireCurrentUserId()
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
