using ACS_View.Application.Interfaces;
using ACS_View.Application.Reminders;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.Infrastructure.Services;

internal sealed class SQLiteNoteReminderStore(IDatabaseService database) : INoteReminderStore
{
    public async Task<IReadOnlyList<StoredNoteReminder>> GetAllAsync() =>
        (await database.Connection.Table<Note>().ToListAsync())
        .Select(n => new StoredNoteReminder(n.Id, n.UserId, n.Content ?? "", n.NotifyOn, n.ReminderMessage)).ToList();

    public async Task SetAsync(StoredNoteReminder note, DateTime? notifyOn, string? message)
    {
        var changed = await database.Connection.ExecuteAsync(
            "UPDATE Note SET NotifyOn = ?, ReminderMessage = ? WHERE Id = ? AND UserId = ?",
            notifyOn, message, note.Id, note.UserId);
        if (changed != 1) throw new InvalidOperationException("A nota foi removida durante a operação do lembrete.");
        DataChangeTracker.MarkNotesChanged();
    }
}
