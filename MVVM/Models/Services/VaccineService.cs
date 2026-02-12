using ACS_View.MVVM.Models.Interfaces;
using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class VaccineService : IVaccineService
    {
        private readonly SQLiteAsyncConnection _database;

        public VaccineService(IDatabaseService dbService)
        {
            _database = dbService.Connection;
        }

        public Task<Vaccines> GetVaccinesBySusAsync(string sus)
            => _database.Table<Vaccines>().FirstOrDefaultAsync(r => r.SusNumber == sus);

        public Task AdicionarVacinasAsync(Vaccines registro)
            => _database.InsertAsync(registro);

        public Task AtualizarVacinasAsync(Vaccines registro)
            => _database.UpdateAsync(registro);
    }
}