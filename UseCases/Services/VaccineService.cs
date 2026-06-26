using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class VaccineService(IDatabaseService dbService, ICurrentUserContext currentUserContext) : IVaccineService
    {
        private readonly SQLiteAsyncConnection _database = dbService.Connection;

        public async Task<Vaccines> GetVaccinesByIdAsync(int id)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            return await _database.Table<Vaccines>().FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        }

        public async Task AdicionarVacinasAsync(Vaccines registro)
        {
            registro.UserId = currentUserContext.RequireCurrentUserId();
            await _database.InsertAsync(registro);
        }

        public async Task AtualizarVacinasAsync(Vaccines registro)
        {
            registro.UserId = currentUserContext.RequireCurrentUserId();
            await _database.UpdateAsync(registro);
        }
    }
}
