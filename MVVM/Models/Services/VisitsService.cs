using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;

namespace ACS_View.MVVM.Models.Services
{
    public class VisitsService : IVisitsService
    {
        private readonly IDatabaseService _databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();

        public async Task<List<Visits>> GetAllVisitsAsync()
        {
            try
            {
                return await _databaseService
                             .Connection
                             .Table<Visits>()
                             .OrderBy(v => v.Date)
                             .ToListAsync() ?? new List<Visits>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar visitas: {ex.Message}");
                return new List<Visits>();
            }
        }

        public async Task RegisterVisitAsync(Visits visit)
        {
            if (visit == null)
                throw new ArgumentNullException(nameof(visit), "A visita não pode ser nula.");

            try
            {
                await _databaseService.Connection.InsertAsync(visit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao adicionar visita: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteVisitAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("O ID da visita deve ser maior que zero.");
            try
            {
                await _databaseService.Connection.ExecuteAsync("DELETE FROM Visits WHERE Id = ?", id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir visita: {ex.Message}");
                throw;
            }
        }
    }
}
