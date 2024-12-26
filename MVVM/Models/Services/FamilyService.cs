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

        public Task<List<Family>> GetAllFamiliesAsync()
        {
            return _connection.Table<Family>().OrderBy(f => f.IdFamilia).ToListAsync();
        }

        public Task<Family> GetFamilyById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID deve ser maior que zero.", nameof(id));

            return _connection.Table<Family>().FirstOrDefaultAsync(h => h.IdFamilia == id);
        }

        public Task<List<HealthRecord>> GetPersonOfFamilyById(int houseId, int familyId)
        {
            if (houseId <= 0)
                throw new ArgumentException($"Indivíduo não encontrado (ID: {houseId})", nameof(houseId));

            return _connection.Table<HealthRecord>().Where(p => p.HouseId == houseId && p.FamilyId == familyId).ToListAsync();
        }

        public Task<int> SaveFamilyAsync(Family family)
        {
            if (family == null)
                throw new ArgumentNullException(nameof(family), "O objeto Família não pode ser nulo.");

            return _connection.InsertAsync(family);
        }

        public Task<int> DeleteFamilyAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID deve ser maior que zero.", nameof(id));

            return _connection.DeleteAsync<Family>(id);
        }

        public async Task UpdateFamily(Family family)
        {
            if (family == null)
                throw new ArgumentNullException(nameof(family), "O objeto Família não pode ser nulo.");

            await _connection.UpdateAsync(family);
        }

        public async Task<int> GetMaxIdAsync(int idHouse)
        {
            if (idHouse <= 0)
                throw new ArgumentException("O ID da residência deve ser maior que zero.", nameof(idHouse));

            try
            {
                // Obtém o maior IdFamilia onde IdHouse é igual ao fornecido
                var maxId = await _connection.Table<HealthRecord>()
                                             .Where(f => f.HouseId == idHouse)
                                             .OrderByDescending(f => f.FamilyId)
                                             .FirstOrDefaultAsync();

                int newFamilyId = maxId?.FamilyId ?? 0; // Se não encontrar, retorna 0

                Console.WriteLine($"Max ID em FamilyService: {newFamilyId}");

                // Retorna o maior IdFamilia ou 0 se não houver registros
                return newFamilyId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter o maior IdFamilia para IdHouse={idHouse}: {ex.Message}");
                throw;
            }
        }

    }
}
