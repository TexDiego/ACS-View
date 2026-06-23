using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces
{
    public interface INoteService
    {
        Task<List<Note>> GetAllNotesAsync();
        Task SaveNoteAsync(Note note);
        Task UpdateNoteAsync(Note note);
        Task DeleteNoteAsync(int id);
    }
}