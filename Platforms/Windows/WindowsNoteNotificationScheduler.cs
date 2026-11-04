using ACS_View.Application.Interfaces;
using ACS_View.Application.Reminders;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ACS_View.Platforms.Windows;

// OS-owned scheduled toasts survive app termination. SQLite remains the source
// of truth; NoteReminderService reconciles OS requests and verifies each write.
internal sealed class WindowsNoteNotificationScheduler(IDialogService dialogs) : INoteNotificationScheduler
{
    private readonly Infrastructure.Services.LocalNoteNotificationScheduler unpackaged = new(dialogs);
    internal static bool HasPackageIdentity
    {
        get
        {
            try { return !string.IsNullOrEmpty(global::Windows.ApplicationModel.Package.Current.Id.Name); }
            catch (Exception) { return false; }
        }
    }
    private const string Group = "acsnotes";
    private const string Marker = "acs:note:";
    private static ToastNotifier Notifier() => ToastNotificationManager.CreateToastNotifier();

    public Task<bool> CanScheduleAsync()
    {
        if (!HasPackageIdentity) return unpackaged.CanScheduleAsync();
        try { return Task.FromResult(Notifier().Setting == NotificationSetting.Enabled); }
        catch (Exception) { return Task.FromResult(false); }
    }

    public async Task<bool> EnsurePermissionAsync()
    {
        if (await CanScheduleAsync()) return true;
        await dialogs.ShowAlertAsync("Notificações do Windows",
            "Ative as notificações do ACS View em Configurações > Sistema > Notificações. Use a versão instalada do aplicativo para agendar lembretes. O lembrete não foi salvo.");
        return false;
    }

    public Task<IReadOnlyList<PendingNoteReminder>> GetPendingAsync()
    {
        if (!HasPackageIdentity) return unpackaged.GetPendingAsync();
        IReadOnlyList<PendingNoteReminder> result = Notifier().GetScheduledToastNotifications()
            .Where(t => t.Group == Group)
            .Select(Read).Where(r => r is not null).Cast<PendingNoteReminder>().ToArray();
        return Task.FromResult(result);
    }

    private static PendingNoteReminder? Read(ScheduledToastNotification toast)
    {
        var launch = toast.Content.DocumentElement.GetAttribute("launch");
        if (!launch.StartsWith(Marker, StringComparison.Ordinal)) return null;
        return JsonSerializer.Deserialize<PendingNoteReminder>(Encoding.UTF8.GetString(Convert.FromBase64String(launch[Marker.Length..])));
    }

    public async Task<bool> ScheduleAsync(PendingNoteReminder reminder)
    {
        if (!HasPackageIdentity) return await unpackaged.ScheduleAsync(reminder);
        if (reminder.NotifyOn <= DateTime.Now || !await CanScheduleAsync()) return false;
        var document = new XmlDocument();
        document.LoadXml("<toast><visual><binding template='ToastGeneric'><text>Lembrete de anotação</text><text/></binding></visual></toast>");
        document.DocumentElement.SetAttribute("launch", Marker + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reminder))));
        document.GetElementsByTagName("text")[1].InnerText = reminder.Message;
        var tag = reminder.Id.ToString(CultureInfo.InvariantCulture);
        var notifier = Notifier();
        foreach (var old in notifier.GetScheduledToastNotifications().Where(t => t.Group == Group && t.Tag == tag))
            notifier.RemoveFromSchedule(old);
        notifier.AddToSchedule(new ScheduledToastNotification(document, new DateTimeOffset(reminder.NotifyOn))
        { Id = tag, Tag = tag, Group = Group });
        return (await GetPendingAsync()).Contains(reminder);
    }

    public Task CancelAsync(int id)
    {
        if (!HasPackageIdentity) return unpackaged.CancelAsync(id);
        var tag = id.ToString(CultureInfo.InvariantCulture);
        var notifier = Notifier();
        foreach (var toast in notifier.GetScheduledToastNotifications().Where(t => t.Group == Group && t.Tag == tag))
            notifier.RemoveFromSchedule(toast);
        ToastNotificationManager.History.Remove(tag, Group);
        if (notifier.GetScheduledToastNotifications().Any(t => t.Group == Group && t.Tag == tag))
            throw new InvalidOperationException("O Windows não confirmou o cancelamento do lembrete.");
        return Task.CompletedTask;
    }
}
