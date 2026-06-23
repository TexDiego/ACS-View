using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class NoteService(IDatabaseService db) : INoteService
    {
        private readonly SQLiteAsyncConnection _database = db.Connection;

        public async Task<List<Note>> GetAllNotesAsync()
        {
            try
            {
                return await _database.QueryAsync<Note>("SELECT * FROM Note ORDER BY CreationDate");
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
                await _database.ExecuteAsync(
                    "INSERT INTO Note (Content, CreationDate) VALUES (?, ?)",
                    note.Content,
                    note.CreationDate
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
            await _database.ExecuteAsync(
                "UPDATE Note SET Content = ?, CreationDate = ?, NotifyOn = ? WHERE Id = ?",
                note.Content,
                note.CreationDate,
                note.NotifyOn,
                note.Id
            );
            DataChangeTracker.MarkNotesChanged();
        }

        public async Task DeleteNoteAsync(int id)
        {
            try
            {
                await _database.ExecuteAsync("DELETE FROM Note WHERE Id = ?", id);
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
