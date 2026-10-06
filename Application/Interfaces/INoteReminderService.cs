namespace ACS_View.Application.Interfaces;

public interface INoteReminderService
{
    Task<bool> EnsurePermissionAsync();
    Task ReconcileAsync();
    Task ScheduleAsync(int noteId, DateTime notifyOn, string message);
    Task CancelAsync(int noteId);
}
