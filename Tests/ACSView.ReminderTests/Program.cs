using ACS_View.Application.Interfaces;
using ACS_View.Application.Reminders;
using ACS_View.UseCases.Services;

var now = new DateTime(2026, 10, 6, 12, 0, 0);
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine($"PASS {name}");
}
async Task Reject(Func<Task> action, string name)
{
    try { await action(); }
    catch (InvalidOperationException) { Check(true, name); return; }
    throw new Exception($"Expected rejection: {name}");
}
var store = new Store();
var scheduler = new Scheduler();
var context = new UserContext();
var service = new NoteReminderService(store, scheduler, context, new Clock(now));
void Reset(int count = 1)
{
    store.Notes.Clear(); scheduler.Pending.Clear(); scheduler.ScheduleCalls = 0;
    scheduler.Enabled = true; scheduler.Accept = true; scheduler.ReportPending = true;
    scheduler.FailCancel = false; scheduler.ThrowScheduleOnce = false; store.FailWriteOnce = false;
    for (var i = 1; i <= count; i++) store.Notes.Add(new(i, 1, "note", null, null));
}

Reset();
await service.ScheduleAsync(1, now.AddMinutes(5), "custom message");
Check(store.Notes[0].NotifyOn == now.AddMinutes(5), "Persist only confirmed future request");
Check(store.Notes[0].Message == "custom message", "Persist custom reminder message for recovery");
Check(scheduler.Pending[1].Id == 1, "Stable legacy integer ID");
await service.ReconcileAsync(); await service.ReconcileAsync();
Check(scheduler.ScheduleCalls == 1, "Reconciliation is idempotent");
scheduler.Pending.Clear();
await service.ReconcileAsync();
Check(scheduler.Pending[1].Message == "custom message", "Recover missing request with original message");
await service.ScheduleAsync(1, now.AddHours(2), "replacement");
Check(scheduler.Pending.Count == 1 && scheduler.Pending[1].NotifyOn == now.AddHours(2), "Reschedule replaces rather than duplicates");
await service.CancelAsync(1);
Check(store.Notes[0].NotifyOn is null && scheduler.Pending.Count == 0, "Cancellation synchronizes SQLite intent and scheduler");

Reset(); scheduler.Accept = false;
await Reject(() => service.ScheduleAsync(1, now.AddMinutes(5), "test"), "False Show result rejected");
Check(store.Notes[0].NotifyOn is null, "False Show result does not persist success");
Reset(); scheduler.ReportPending = false;
await Reject(() => service.ScheduleAsync(1, now.AddMinutes(5), "test"), "Absent pending confirmation rejected");
Check(store.Notes[0].NotifyOn is null && scheduler.Pending.Count == 0, "Unconfirmed request rolled back");
Reset(); scheduler.Enabled = false;
await Reject(() => service.ScheduleAsync(1, now.AddMinutes(5), "test"), "Disabled permission rejected");
Check(store.Notes[0].NotifyOn is null && scheduler.ScheduleCalls == 0, "Disabled permissions leave state untouched");
Reset();
await Reject(() => service.ScheduleAsync(1, now, "test"), "Past or current time rejected");
await service.ScheduleAsync(1, now.AddHours(1), "old");
store.FailWriteOnce = true;
await Reject(() => service.ScheduleAsync(1, now.AddHours(2), "new"), "Database failure reported");
Check(store.Notes[0].NotifyOn == now.AddHours(1) && scheduler.Pending[1].Message == "old", "Database failure restores previous request");
scheduler.ThrowScheduleOnce = true;
await Reject(() => service.ScheduleAsync(1, now.AddHours(2), "new"), "Scheduler exception reported");
Check(scheduler.Pending[1].Message == "old", "Scheduler exception restores previous request");
scheduler.FailCancel = true;
await Reject(() => service.CancelAsync(1), "Cancellation failure reported");
Check(store.Notes[0].NotifyOn == now.AddHours(1), "Cancellation failure restores persisted intent");

Reset(11);
for (var i = 1; i <= 10; i++) await service.ScheduleAsync(i, now.AddHours(1), "reminder");
await Reject(() => service.ScheduleAsync(11, now.AddHours(1), "extra"), "Eleventh reminder rejected");
await service.ScheduleAsync(1, now.AddHours(2), "replace");
Check(scheduler.Pending.Count == 10, "Replacement allowed at limit");
await service.CancelAsync(2); await service.ScheduleAsync(11, now.AddHours(1), "extra");
Check(scheduler.Pending.Count == 10, "Cancellation releases limit slot");

Reset(2);
store.Notes[0] = store.Notes[0] with { NotifyOn = now.AddMinutes(-1) };
scheduler.Pending[1] = new(1, now.AddMinutes(-1), "expired");
scheduler.Pending[99] = new(99, now.AddHours(1), "orphan");
scheduler.Pending[-1] = new(-1, now.AddHours(1), "other category");
await service.ReconcileAsync();
Check(store.Notes[0].NotifyOn is null && !scheduler.Pending.ContainsKey(1), "Expired reminder cleared without rescheduling");
Check(!scheduler.Pending.ContainsKey(99) && scheduler.Pending.ContainsKey(-1), "Orphan removed and other category preserved");
store.Notes[0] = store.Notes[0] with { NotifyOn = now.AddHours(1) };
scheduler.Pending[1] = new(1, now.AddHours(2), "interrupted");
await service.ReconcileAsync();
Check(scheduler.Pending[1].NotifyOn == now.AddHours(1), "Persisted time repairs interrupted reschedule");
store.Notes[0] = store.Notes[0] with { Message = "persisted custom text" };
scheduler.Pending[1] = new(1, now.AddHours(1), "stale plugin text");
await service.ReconcileAsync();
Check(scheduler.Pending[1].Message == "persisted custom text", "Persisted message repairs stale request at the same time");
await service.ReconcileAsync();
Check(scheduler.Pending.Count(p => p.Key == 1) == 1, "Message repair preserves the stable request ID");
store.Notes[1] = store.Notes[1] with { UserId = 2, NotifyOn = now.AddHours(1) };
await service.ReconcileAsync();
Check(scheduler.Pending.ContainsKey(2), "Recovery handles persisted reminders without changing user session");
await Reject(() => service.CancelAsync(2), "Cannot cancel another user's note");
await Reject(() => service.ScheduleAsync(2, now.AddHours(2), "test"), "Cannot schedule another user's note");

Reset(11);
await Task.WhenAll(Enumerable.Range(1, 9).Select(i => service.ScheduleAsync(i, now.AddHours(1), "test")));
var results = await Task.WhenAll(new[] { 10, 11 }.Select(async i =>
{
    try { await service.ScheduleAsync(i, now.AddHours(1), "test"); return true; }
    catch (InvalidOperationException) { return false; }
}));
Check(results.Count(r => r) == 1 && scheduler.Pending.Count == 10, "Concurrent scheduling cannot bypass limit");
Console.WriteLine($"{checks} reminder checks passed. Android delivery is not simulated.");

sealed class Clock(DateTime now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
sealed class Store : INoteReminderStore
{
    public List<StoredNoteReminder> Notes { get; } = [];
    public bool FailWriteOnce { get; set; }
    public Task<IReadOnlyList<StoredNoteReminder>> GetAllAsync() => Task.FromResult<IReadOnlyList<StoredNoteReminder>>(Notes.ToList());
    public Task SetAsync(StoredNoteReminder note, DateTime? notifyOn, string? message)
    {
        if (FailWriteOnce) { FailWriteOnce = false; throw new InvalidOperationException("write failed"); }
        var index = Notes.FindIndex(n => n.Id == note.Id && n.UserId == note.UserId);
        Notes[index] = Notes[index] with { NotifyOn = notifyOn, Message = message };
        return Task.CompletedTask;
    }
}
sealed class Scheduler : INoteNotificationScheduler
{
    public Dictionary<int, PendingNoteReminder> Pending { get; } = [];
    public bool Enabled { get; set; } = true;
    public bool Accept { get; set; } = true;
    public bool ReportPending { get; set; } = true;
    public bool FailCancel { get; set; }
    public bool ThrowScheduleOnce { get; set; }
    public int ScheduleCalls { get; set; }
    public Task<bool> EnsurePermissionAsync() => Task.FromResult(Enabled);
    public Task<bool> CanScheduleAsync() => Task.FromResult(Enabled);
    public Task<IReadOnlyList<PendingNoteReminder>> GetPendingAsync() =>
        Task.FromResult<IReadOnlyList<PendingNoteReminder>>(ReportPending ? Pending.Values.ToList() : []);
    public Task<bool> ScheduleAsync(PendingNoteReminder reminder)
    {
        ScheduleCalls++;
        if (ThrowScheduleOnce) { ThrowScheduleOnce = false; throw new InvalidOperationException("schedule failed"); }
        if (Accept) Pending[reminder.Id] = reminder;
        return Task.FromResult(Accept);
    }
    public Task CancelAsync(int id)
    {
        if (FailCancel) throw new InvalidOperationException("cancel failed");
        Pending.Remove(id); return Task.CompletedTask;
    }
}
sealed class UserContext : ICurrentUserContext
{
    public int CurrentUserId { get; private set; } = 1;
    public bool HasCurrentUser => CurrentUserId > 0;
    public void SetCurrentUser(int userId) => CurrentUserId = userId;
    public void Clear() => CurrentUserId = 0;
    public int RequireCurrentUserId() => HasCurrentUser ? CurrentUserId : throw new InvalidOperationException("No user");
}
