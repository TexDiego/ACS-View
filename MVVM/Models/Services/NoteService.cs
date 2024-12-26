using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    public class NoteService(DatabaseService dbService)
    {
        private readonly SQLiteAsyncConnection _database = dbService.GetConnection();

        public Task<List<Note>> GetAllNotesAsync() => _database.Table<Note>().OrderBy(n => n.CreationDate).ToListAsync();

        public Task<int> SaveNoteAsync(Note note) => _database.InsertAsync(note);

        public Task<int> DeleteNoteAsync(int id) => _database.DeleteAsync<Note>(id);

    }
}
