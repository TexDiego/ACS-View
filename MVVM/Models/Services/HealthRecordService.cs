using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using SQLite;
using System.Diagnostics;
using System.Linq.Expressions;

namespace ACS_View.MVVM.Models.Services
{
    internal class HealthRecordService : IHealthRecordService
    {
        private readonly IDatabaseService _databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();
        private readonly SQLiteAsyncConnection _database;

        public HealthRecordService()
        {
            _database = _databaseService.Connection;
        }

        public async Task<List<Patient>> GetAllRecordsAsync()
        {
            return await _database.Table<Patient>()
                                  .OrderBy(r => r.Name)
                                  .ToListAsync();
        }

        public async Task<Patient?> GetRecordBySusAsync(string sus)
        {
            return await _database.Table<Patient>().FirstOrDefaultAsync(r => r.SusNumber == sus);
        }

        public async Task<List<Patient>> GetRecordByNameOrSusAsync(string search)
        {
            string lowerSearch = search.ToLower();
            return await _database.Table<Patient>()
                                  .Where(p => p.Name.ToLower().Contains(lowerSearch)
                                           || p.SusNumber.Contains(search))
                                  .OrderBy(p => p.Name)
                                  .Take(10)
                                  .ToListAsync();
        }

        public Task<int> SaveRecordAsync(Patient record)
        {
            return _database.InsertOrReplaceAsync(record);
        }

        public async Task AddRecordAsync(Patient record)
        {
            await _database.InsertAsync(record);
        }

        public async Task UpdateRecordAsync(Patient record)
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

        public Task<int> GetConditionCountAsync(Expression<Func<Patient, bool>> condition)
        {
            return _database.Table<Patient>().CountAsync(condition);
        }

        public Task<int> GetTotalCountAsync()
        {
            return _database.Table<Patient>().CountAsync();
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

        private static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            int age = today.Year - birthDate.Year;
            if (today.Month < birthDate.Month || (today.Month == birthDate.Month && today.Day < birthDate.Day))
                age--;
            return age;
        }

        public async Task<List<Patient>> GetRecordsByFamilyAndHouseAsync(int idFamily, int idHouse)
        {
            return await _database.Table<Patient>()
                                  .Where(record => record.FamilyId == idFamily && record.HouseId == idHouse)
                                  .ToListAsync();
        }

        public async Task<List<Patient>> GetRecordsByHouseIdAsync(int idHouse)
        {
            try
            {
                var records = await _database.Table<Patient>()
                                             .Where(p => p.HouseId == idHouse)
                                             .OrderBy(p => p.Name)
                                             .ToListAsync();

                if (records.Count == 0)
                    Debug.WriteLine("Nenhum registro encontrado no banco para o HouseId especificado.");
                else
                    foreach (var record in records)
                        Debug.WriteLine($"Registro encontrado: Nome={record.Name}, HouseId={record.HouseId}, FamilyId={record.FamilyId}");

                return records;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar registros no banco: {ex.Message}");
                return [];
            }
        }
    }
}