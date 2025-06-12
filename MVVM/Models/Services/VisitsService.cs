using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class VisitsService
    {
        private readonly SQLiteAsyncConnection _connection;

        public VisitsService(DatabaseService databaseService)
        {
            _connection = databaseService?.GetConnection()
                          ?? throw new ArgumentNullException(nameof(databaseService));
        }

        public async Task<List<Visits>> GetAllVisitsAsync()
        {
            try
            {
                return await _connection.Table<Visits>()
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
                await _connection.InsertAsync(visit);
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
                await _connection.ExecuteAsync("DELETE FROM Visits WHERE Id = ?", id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir visita: {ex.Message}");
                throw;
            }
        }
    }
}
