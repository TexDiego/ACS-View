using ACS_View.Application.Interfaces;
using ACS_View.Application.Reminders;
using ACS_View.Views;
using Plugin.LocalNotification;

namespace ACS_View.Infrastructure.Services;

internal sealed class LocalNoteNotificationScheduler(IDialogService dialogs) : INoteNotificationScheduler
{
    public const string ChannelId = "acs_note_reminders";
    private const string Marker = "acs:note:";
    internal const string ExactAlarmCacheInvalid = "acs_note_exact_alarm_cache_invalid";
    private static readonly NotificationPermission NotificationOnly = new()
    {
        Android = new() { RequestPermissionToScheduleExactAlarm = false }
    };

    private static bool ExactAlarmsEnabled()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var alarms = (global::Android.App.AlarmManager?)global::Android.App.Application.Context
                .GetSystemService(global::Android.Content.Context.AlarmService);
            var enabled = alarms?.CanScheduleExactAlarms() == true;
            if (!enabled) Preferences.Default.Set(ExactAlarmCacheInvalid, true);
            return enabled;
        }
#endif
        return true;
    }

    private static bool ChannelEnabled()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var manager = (global::Android.App.NotificationManager?)global::Android.App.Application.Context
                .GetSystemService(global::Android.Content.Context.NotificationService);
            var channel = manager?.GetNotificationChannel(ChannelId);
            return channel is not null && channel.Importance != global::Android.App.NotificationImportance.None;
        }
#endif
        return true;
    }

    public async Task<bool> CanScheduleAsync() =>
        await LocalNotificationCenter.Current.AreNotificationsEnabled(NotificationOnly)
        && ChannelEnabled() && ExactAlarmsEnabled();

    public async Task<bool> EnsurePermissionAsync()
    {
        // The plugin owns the POST_NOTIFICATIONS/iOS prompt; don't ask through MAUI again.
        using var interaction = (Microsoft.Maui.Controls.Application.Current as App)?.BeginExternalInteraction();
        await LocalNotificationCenter.Current.RequestNotificationPermission(NotificationOnly);
        if (!await LocalNotificationCenter.Current.AreNotificationsEnabled(NotificationOnly) || !ChannelEnabled())
        {
            await dialogs.ShowAlertAsync("Notificações desativadas",
                "Ative as notificações do ACS View e o canal Lembretes de notas nas configurações do aparelho. O lembrete não foi agendado.");
            return false;
        }
        if (ExactAlarmsEnabled()) return true;

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(31) && await dialogs.ShowConfirmationAsync(
            "Alarmes e lembretes",
            "As notificações estão permitidas, mas o Android ainda não permite agendar no horário escolhido. Autorize o ACS View em Alarmes e lembretes. Ao voltar, o aplicativo verificará o acesso antes de salvar.",
            "Abrir configurações"))
        {
            var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
            if (window is null) return false;
            var resumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnResumed(object? sender, EventArgs args) => resumed.TrySetResult();
            window.Resumed += OnResumed;
            try
            {
                var context = global::Android.App.Application.Context;
                var intent = new global::Android.Content.Intent(
                    global::Android.Provider.Settings.ActionRequestScheduleExactAlarm,
                    global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
                intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                context.StartActivity(intent);
                await resumed.Task;
            }
            finally { window.Resumed -= OnResumed; }
            if (await CanScheduleAsync()) return true;
        }
#endif
        await dialogs.ShowAlertAsync("Horário exato não autorizado",
            "O lembrete não foi salvo. Permita Alarmes e lembretes para o ACS View e tente novamente.");
        return false;
    }

    public async Task<IReadOnlyList<PendingNoteReminder>> GetPendingAsync()
    {
        var pending = (await LocalNotificationCenter.Current.GetPendingNotificationList())
        // Legacy requests used positive note IDs and this title, without ReturningData.
        .Where(r => r.NotificationId > 0 && r.Schedule.NotifyTime.HasValue &&
            (r.ReturningData == Marker + r.NotificationId ||
             (string.IsNullOrEmpty(r.ReturningData) && r.Title == "Lembrete de anotação")))
        .Select(r => new PendingNoteReminder(r.NotificationId, r.Schedule.NotifyTime!.Value, r.Description)).ToList();
#if ANDROID
        // Revoking exact-alarm access removes AlarmManager alarms but not the plugin's
        // SharedPreferences list. A subsequent grant invalidates that stale cache.
        if (Preferences.Default.Get(ExactAlarmCacheInvalid, false) && ExactAlarmsEnabled())
        {
            foreach (var reminder in pending)
                if (!LocalNotificationCenter.Current.Cancel(reminder.Id))
                    throw new InvalidOperationException("Não foi possível limpar um agendamento invalidado pelo Android.");
            Preferences.Default.Remove(ExactAlarmCacheInvalid);
            return [];
        }
#endif
        return pending;
    }

    public async Task<bool> ScheduleAsync(PendingNoteReminder reminder)
    {
        if (!await CanScheduleAsync()) return false;
        var request = new NotificationRequest
        {
            NotificationId = reminder.Id,
            ReturningData = Marker + reminder.Id,
            Title = "Lembrete de anotação",
            Description = reminder.Message,
            Android = new() { ChannelId = ChannelId },
            Schedule = new() { NotifyTime = reminder.NotifyOn }
        };
        return await LocalNotificationCenter.Current.Show(request);
    }

    public async Task CancelAsync(int id)
    {
        if (!LocalNotificationCenter.Current.Cancel(id) || (await GetPendingAsync()).Any(r => r.Id == id))
            throw new InvalidOperationException("O sistema não confirmou o cancelamento do lembrete.");
    }
}
