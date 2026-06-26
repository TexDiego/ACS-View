using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class NoteService(IDatabaseService db, ICurrentUserContext currentUserContext) : INoteService
    {
        private readonly SQLiteAsyncConnection _database = db.Connection;

        public async Task<List<Note>> GetAllNotesAsync()
        {
            try
            {
                var userId = currentUserContext.RequireCurrentUserId();
                return await _database.QueryAsync<Note>("SELECT * FROM Note WHERE UserId = ? ORDER BY CreationDate", userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar notas: {ex.Message}");
                return new List<Note>();
            }
        }

        public async Task SaveNoteAsync(Note note)
        {
            try
            {
                var userId = currentUserContext.RequireCurrentUserId();
                await _database.ExecuteAsync(
                    "INSERT INTO Note (UserId, Content, CreationDate, NotifyOn) VALUES (?, ?, ?, ?)",
                    userId,
                    note.Content,
                    note.CreationDate,
                    note.NotifyOn
                );
                DataChangeTracker.MarkNotesChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar nota: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateNoteAsync(Note note)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            await _database.ExecuteAsync(
                "UPDATE Note SET Content = ?, CreationDate = ?, NotifyOn = ? WHERE Id = ? AND UserId = ?",
                note.Content,
                note.CreationDate,
                note.NotifyOn,
                note.Id,
                userId
            );
            DataChangeTracker.MarkNotesChanged();
        }

        public async Task DeleteNoteAsync(int id)
        {
            try
            {
                await _database.ExecuteAsync("DELETE FROM Note WHERE Id = ? AND UserId = ?", id, currentUserContext.RequireCurrentUserId());
                DataChangeTracker.MarkNotesChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar nota: {ex.Message}");
                throw;
            }
        }
    }
}
