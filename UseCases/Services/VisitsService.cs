using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class VisitsService(IDatabaseService db, ICurrentUserContext currentUserContext) : IVisitsService
    {
        private readonly SQLiteAsyncConnection _connection = db.Connection;

        public async Task<List<Visits>> GetAllVisitsAsync()
        {
            try
            {
                var userId = currentUserContext.RequireCurrentUserId();
                return await _connection
                             .Table<Visits>()
                             .Where(v => v.UserId == userId)
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
                visit.UserId = currentUserContext.RequireCurrentUserId();
                await _connection.InsertAsync(visit);
                DataChangeTracker.MarkVisitsChanged();
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
                await _connection.ExecuteAsync("DELETE FROM Visits WHERE Id = ? AND UserId = ?", id, currentUserContext.RequireCurrentUserId());
                DataChangeTracker.MarkVisitsChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir visita: {ex.Message}");
                throw;
            }
        }
    }
}
