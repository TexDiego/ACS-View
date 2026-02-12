namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface INoteService
    {
        Task<List<Note>> GetAllNotesAsync();
        Task<int> SaveNoteAsync(Note note);
        Task<int> DeleteNoteAsync(int id);
    }
}