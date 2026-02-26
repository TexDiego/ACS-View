using ACS_View.Domain.Entities;

namespace ACS_View.Domain.Interfaces
{
    internal interface INoteService
    {
        Task<List<Note>> GetAllNotesAsync();
        Task<int> SaveNoteAsync(Note note);
        Task<int> DeleteNoteAsync(int id);
    }
}