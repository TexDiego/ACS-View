using ACS_View.MVVM.Models.Interfaces;
using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class VaccineService(IDatabaseService dbService) : IVaccineService
    {
        private readonly SQLiteAsyncConnection _database = dbService.Connection;

        public async Task<Vaccines> GetVaccinesByIdAsync(int id)
            => await _database.Table<Vaccines>().FirstOrDefaultAsync(r => r.Id == id);

        public async Task AdicionarVacinasAsync(Vaccines registro)
            => await _database.InsertAsync(registro);

        public async Task AtualizarVacinasAsync(Vaccines registro)
            => await _database.UpdateAsync(registro);
    }
}