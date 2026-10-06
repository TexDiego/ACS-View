namespace ACS_View.Application.Reminders;

public sealed record StoredNoteReminder(int Id, int UserId, string Content, DateTime? NotifyOn, string? Message);
public sealed record PendingNoteReminder(int Id, DateTime NotifyOn, string Message);

public interface INoteReminderStore
{
    Task<IReadOnlyList<StoredNoteReminder>> GetAllAsync();
    Task SetAsync(StoredNoteReminder note, DateTime? notifyOn, string? message);
}

public interface INoteNotificationScheduler
{
    Task<bool> EnsurePermissionAsync();
    Task<bool> CanScheduleAsync();
    Task<IReadOnlyList<PendingNoteReminder>> GetPendingAsync();
    Task<bool> ScheduleAsync(PendingNoteReminder reminder);
    Task CancelAsync(int id);
}
