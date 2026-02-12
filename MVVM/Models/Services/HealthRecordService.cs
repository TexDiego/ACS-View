using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using SQLite;
using System.Linq.Expressions;

namespace ACS_View.MVVM.Models.Services
{
    public class HealthRecordService : IHealthRecordService
    {
        private readonly IDatabaseService _databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();
        private readonly SQLiteAsyncConnection _database;

        public HealthRecordService()
        {
            _database = _databaseService.Connection;
        }

        public async Task<List<HealthRecord>> GetAllRecordsAsync()
        {
            return await _database.Table<HealthRecord>()
                                  .OrderBy(r => r.Name)
                                  .ToListAsync();
        }

        public async Task<HealthRecord?> GetRecordBySusAsync(string sus)
        {
            return await _database.Table<HealthRecord>()
                                  .FirstOrDefaultAsync(r => r.SusNumber == sus);
        }

        public async Task<List<HealthRecord>> GetRecordByNameOrSusAsync(string search)
        {
            string lowerSearch = search.ToLower();
            return await _database.Table<HealthRecord>()
                                  .Where(p => p.Name.ToLower().Contains(lowerSearch)
                                           || p.SusNumber.Contains(search))
                                  .OrderBy(p => p.Name)
                                  .Take(10)
                                  .ToListAsync();
        }

        public Task<int> SaveRecordAsync(HealthRecord record)
        {
            return _database.InsertOrReplaceAsync(record);
        }

        public async Task AddRecordAsync(HealthRecord record)
        {
            await _database.InsertAsync(record);
        }

        public async Task UpdateRecordAsync(HealthRecord record)
        {
            await _database.UpdateAsync(record);
        }

        public async Task DeleteRecordAsync(string sus)
        {
            try
            {
                var recordToDelete = await GetRecordBySusAsync(sus);
                if (recordToDelete != null)
                {
                    await _database.DeleteAsync(recordToDelete);
                    Console.WriteLine("Registro deletado do banco de dados.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar do banco de dados: {ex.Message}");
                throw;
            }
        }

        public Task<int> GetConditionCountAsync(Expression<Func<HealthRecord, bool>> condition)
        {
            return _database.Table<HealthRecord>().CountAsync(condition);
        }

        public Task<int> GetTotalCountAsync()
        {
            return _database.Table<HealthRecord>().CountAsync();
        }

        public async Task<int> GetElderCountAsync()
        {
            var records = await GetAllRecordsAsync();
            return records.Count(r => CalculateAge(r.BirthDate) >= 60);
        }

        public async Task<int> GetYoungerCountAsync()
        {
            var records = await GetAllRecordsAsync();
            return records.Count(r => CalculateAge(r.BirthDate) < 6);
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            int age = today.Year - birthDate.Year;
            if (today.Month < birthDate.Month || (today.Month == birthDate.Month && today.Day < birthDate.Day))
                age--;
            return age;
        }

        public async Task<List<HealthRecord>> GetRecordsByFamilyAndHouseAsync(int idFamily, int idHouse)
        {
            return await _database.Table<HealthRecord>()
                                  .Where(record => record.FamilyId == idFamily && record.HouseId == idHouse)
                                  .ToListAsync();
        }

        public async Task<List<HealthRecord>> GetRecordsByHouseIdAsync(int idHouse)
        {
            try
            {
                var records = await _database.Table<HealthRecord>()
                                             .Where(p => p.HouseId == idHouse)
                                             .OrderBy(p => p.Name)
                                             .ToListAsync();

                if (!records.Any())
                    Console.WriteLine("Nenhum registro encontrado no banco para o HouseId especificado.");
                else
                    foreach (var record in records)
                        Console.WriteLine($"Registro encontrado: Nome={record.Name}, HouseId={record.HouseId}, FamilyId={record.FamilyId}");

                return records;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar registros no banco: {ex.Message}");
                return new List<HealthRecord>();
            }
        }
    }
}