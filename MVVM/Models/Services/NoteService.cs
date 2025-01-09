using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ACS_View.MVVM.Models.Services
{
    public class NoteService
    {
        private readonly SQLiteAsyncConnection _database;

        public NoteService(DatabaseService dbService)
        {
            _database = dbService.GetConnection();
        }

        public async Task<List<Note>> GetAllNotesAsync()
        {
            try
            {
                // Consulta SQL direta para buscar todas as notas, ordenadas pela data de criação
                return await _database.QueryAsync<Note>("SELECT * FROM Note ORDER BY CreationDate");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar notas: {ex.Message}");
                return new List<Note>();
            }
        }

        public async Task<int> SaveNoteAsync(Note note)
        {
            try
            {
                // Inserção direta de uma nota
                return await _database.ExecuteAsync(
                    "INSERT INTO Note (Content, CreationDate) VALUES (?, ?)",
                    note.Content,
                    note.CreationDate
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar nota: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteNoteAsync(int id)
        {
            try
            {
                // Exclusão direta de uma nota pelo ID
                return await _database.ExecuteAsync("DELETE FROM Note WHERE Id = ?", id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar nota: {ex.Message}");
                throw;
            }
        }
    }
}
