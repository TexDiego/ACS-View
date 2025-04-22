using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class VaccineService(DatabaseService dbService)
    {
        private readonly SQLiteAsyncConnection _database = dbService.GetConnection();

        public Task<Vaccines> GetVaccinesBySusAsync(string sus) => _database.Table<Vaccines>().FirstOrDefaultAsync(r => r.SusNumber == sus);

        public async Task AdicionarVacinasAsync(Vaccines registro)
        {
            await _database.InsertAsync(registro);
        }

        public async Task AtualizarVacinasAsync(Vaccines registro)
        {
            await _database.UpdateAsync(registro);
        }
    }
}
