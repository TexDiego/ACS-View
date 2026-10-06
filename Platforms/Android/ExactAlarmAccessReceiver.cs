using ACS_View.Application.Interfaces;
using ACS_View.Infrastructure.Services;
using Android.App;
using Android.Content;

namespace ACS_View.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(["android.app.action.SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED"])]
public sealed class ExactAlarmAccessReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != "android.app.action.SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED") return;
        Preferences.Default.Set(LocalNoteNotificationScheduler.ExactAlarmCacheInvalid, true);
        var pendingResult = GoAsync();
        _ = RestoreAsync(pendingResult);
    }

    private static async Task RestoreAsync(PendingResult? pendingResult)
    {
        try
        {
            var services = IPlatformApplication.Current?.Services;
            if (services is null) return; // The next app startup reconciles the durable flag.
            SQLitePCL.Batteries_V2.Init();
            await services.GetRequiredService<IAppStartupService>().InitializeAsync();
            await services.GetRequiredService<INoteReminderService>().ReconcileAsync();
        }
        catch (Exception)
        {
            System.Diagnostics.Debug.WriteLine("Não foi possível restaurar lembretes após autorização de alarmes.");
        }
        finally { pendingResult?.Finish(); }
    }
}
