using SQLite;
using System.Linq.Expressions;

namespace ACS_View.MVVM.Models.Services
{
    public class HealthRecordService
    {
        private readonly SQLiteAsyncConnection _database;

        public HealthRecordService(DatabaseService dbService)
        {
            _database = dbService.GetConnection();
        }

        public Task<List<HealthRecord>> GetAllRecordsAsync() => _database.Table<HealthRecord>().OrderBy(r => r.Name).ToListAsync();

        public Task<HealthRecord> GetRecordBySusAsync(string susNumber) =>
            _database.Table<HealthRecord>().FirstOrDefaultAsync(r => r.SusNumber == susNumber);

        public Task<List<HealthRecord>> GetRecordByCondition(string condition) =>
            _database.Table<HealthRecord>().Where(r => r.Equals(condition)).OrderBy(r => r.Name).ToListAsync();

        public Task<int> SaveRecordAsync(HealthRecord record) => _database.InsertOrReplaceAsync(record);

        public async Task AdicionarCadastroAsync(HealthRecord registro)
        {
            await _database.InsertAsync(registro);
        }

        public async Task AtualizarCadastroAsync(HealthRecord registro)
        {
            await _database.UpdateAsync(registro);
        }

        public async Task DeleteRecordAsync(string susNumber)
        {
            try
            {
                var recordToDelete = await _database.Table<HealthRecord>().FirstOrDefaultAsync(r => r.SusNumber == susNumber);
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

        public async Task<int> GetConditionCountAsync(Expression<Func<HealthRecord, bool>> condition)
        {
            return await _database.Table<HealthRecord>().CountAsync(condition);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _database.Table<HealthRecord>().CountAsync();
        }

        public async Task<int> GetElder()
        {
            var records = await GetAllRecordsAsync();

            return records.Count(r =>
            {
                var age = DateTime.Today.Year - r.BirthDate.Year;

                // Se o mês atual for anterior ao mês de nascimento, ou se for o mesmo mês, mas o dia ainda não chegou, subtrai 1 do cálculo da idade.
                if (DateTime.Today.Month < r.BirthDate.Month || (DateTime.Today.Month == r.BirthDate.Month && DateTime.Today.Day < r.BirthDate.Day))
                {
                    age--;
                }

                return age >= 60;
            });
        }

        public async Task<int> GetYoungers()
        {
            var records = await GetAllRecordsAsync();

            return records.Count(r =>
            {
                var age = DateTime.Today.Year - r.BirthDate.Year;

                if (DateTime.Today.Month < r.BirthDate.Month || (DateTime.Today.Month == r.BirthDate.Month && DateTime.Today.Day < r.BirthDate.Day))
                {
                    age--;
                }

                return age < 2;
            });
        }
    }
}
