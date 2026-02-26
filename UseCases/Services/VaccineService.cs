using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class VaccineService(IDatabaseService dbService) : IVaccineService
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