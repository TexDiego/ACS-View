using ACS_View.Application.Interfaces;
using ACS_View.Application.Reminders;

namespace ACS_View.UseCases.Services;

// Positive SQLite note IDs retain the old Int32.GetHashCode IDs. Reserve nonpositive
// IDs for future categories; never use random/hash/string IDs for notifications.
public sealed class NoteReminderService(
    INoteReminderStore store,
    INoteNotificationScheduler scheduler,
    ICurrentUserContext currentUser,
    TimeProvider clock) : INoteReminderService
{
    public const int MaxActive = 10;
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTime Now => clock.GetLocalNow().DateTime;

    public Task<bool> EnsurePermissionAsync() => scheduler.EnsurePermissionAsync();

    public async Task ReconcileAsync()
    {
        await gate.WaitAsync();
        try { await ReconcileCoreAsync(); }
        finally { gate.Release(); }
    }

    private async Task ReconcileCoreAsync()
    {
        var notes = await store.GetAllAsync();
        var pending = await scheduler.GetPendingAsync();
        var now = Now;
        var future = notes.Where(n => n.NotifyOn > now).ToDictionary(n => n.Id);

        // Remove orphaned requests, including those left by bulk note deletion.
        foreach (var item in pending.Where(p => p.Id > 0 && !future.ContainsKey(p.Id)))
            await scheduler.CancelAsync(item.Id);

        foreach (var note in notes.Where(n => n.NotifyOn <= now))
        {
            await store.SetAsync(note, null, null);
            // Cancel pending alarms only; keep already delivered notifications visible.
        }

        if (!await scheduler.CanScheduleAsync()) return;
        foreach (var note in future.Values)
        {
            var existing = pending.FirstOrDefault(p => p.Id == note.Id);
            // Timestamp comparison also repairs an interrupted reschedule/SQLite write.
            var request = ToPending(note);
            if (existing == request) continue;
            if (!await scheduler.ScheduleAsync(request) || !await IsPendingAsync(request))
                throw new InvalidOperationException("Não foi possível restaurar um lembrete pendente. Tente novamente ao abrir as notas.");
        }
    }

    public async Task ScheduleAsync(int noteId, DateTime notifyOn, string message)
    {
        await gate.WaitAsync();
        try
        {
            if (notifyOn <= Now) throw new InvalidOperationException("Escolha um horário no futuro.");
            if (!await scheduler.CanScheduleAsync())
                throw new InvalidOperationException("Confira as permissões de notificações e de Alarmes e lembretes.");
            await ReconcileCoreAsync();
            var notes = await store.GetAllAsync();
            var note = OwnedNote(notes, noteId);
            if (notes.Count(n => n.UserId == note.UserId && n.Id != noteId && n.NotifyOn > Now) >= MaxActive)
                throw new InvalidOperationException("É possível manter no máximo 10 lembretes ativos das suas notas. Cancele um antes de adicionar outro.");
            var previous = (await scheduler.GetPendingAsync()).FirstOrDefault(p => p.Id == noteId);
            var request = new PendingNoteReminder(noteId, notifyOn, message);
            try
            {
                // Same ID replaces the previous alarm; don't cancel it before validation.
                if (!await scheduler.ScheduleAsync(request) || !await IsPendingAsync(request))
                    throw new InvalidOperationException("O sistema não confirmou o agendamento. O lembrete não foi salvo.");
                await store.SetAsync(note, notifyOn, message);
            }
            catch
            {
                // SQLite still contains the prior reminder. Restore its OS counterpart.
                await scheduler.CancelAsync(noteId);
                if (previous is not null && previous.NotifyOn > Now)
                {
                    if (!await scheduler.ScheduleAsync(previous) || !await IsPendingAsync(previous))
                        throw new InvalidOperationException("O agendamento falhou e o lembrete anterior precisa ser restaurado. Abra as notas novamente.");
                }
                throw;
            }
        }
        finally { gate.Release(); }
    }

    public async Task CancelAsync(int noteId)
    {
        await gate.WaitAsync();
        try
        {
            var note = OwnedNote(await store.GetAllAsync(), noteId);
            // Persist first, so startup can remove an alarm left by process termination.
            await store.SetAsync(note, null, null);
            try { await scheduler.CancelAsync(noteId); }
            catch
            {
                await store.SetAsync(note, note.NotifyOn, note.Message);
                throw;
            }
        }
        finally { gate.Release(); }
    }

    private StoredNoteReminder OwnedNote(IReadOnlyList<StoredNoteReminder> notes, int id) =>
        notes.FirstOrDefault(n => n.Id == id && n.Id > 0 && n.UserId == currentUser.RequireCurrentUserId())
        ?? throw new InvalidOperationException("Nota não encontrada para o usuário atual.");

    private static PendingNoteReminder ToPending(StoredNoteReminder note) =>
        new(note.Id, note.NotifyOn!.Value, note.Message ?? note.Content[..Math.Min(note.Content.Length, 160)]);

    private async Task<bool> IsPendingAsync(PendingNoteReminder request) =>
        (await scheduler.GetPendingAsync()).Any(p => p == request);
}
