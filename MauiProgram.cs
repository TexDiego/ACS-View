using ACS_View.Infrastructure.DependencyInjection;
using ACS_View.Views;
using CommunityToolkit.Maui;
using MauiIcons.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using SQLite;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using ACS_View.Infrastructure.Services;

namespace ACS_View
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
#if ANDROID
            // Android can remove alarms on force-stop while the plugin keeps its
            // persisted request list. Rebuild future reminders on process startup.
            Preferences.Default.Set(LocalNoteNotificationScheduler.ExactAlarmCacheInvalid, true);
#endif
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseLocalNotification(config => config.AddAndroid(android => android.AddChannel(new NotificationChannelRequest
                {
                    Id = LocalNoteNotificationScheduler.ChannelId,
                    Name = "Lembretes de notas",
                    Description = "Lembretes agendados das suas anotações no ACS View",
                    Importance = AndroidImportance.Default,
                    CanBypassDnd = false
                })))
                .UseFluentMauiIcons()
                .UseMauiCommunityToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("ClearInitialFocus", (handler, _) =>
                    {
                        handler.PlatformView.Post(() => handler.PlatformView.ClearFocus());
                    });

                    Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("ClearInitialFocus", (handler, _) =>
                    {
                        handler.PlatformView.Post(() => handler.PlatformView.ClearFocus());
                    });

                    Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("ClearInitialFocus", (handler, _) =>
                    {
                        handler.PlatformView.Post(() => handler.PlatformView.ClearFocus());
                    });
#endif
                })
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                    events.AddAndroid(android => android.OnCreate((activity, _) =>
                    {
                        activity.Window?.SetSoftInputMode(
                            Android.Views.SoftInput.StateAlwaysHidden |
                            Android.Views.SoftInput.AdjustResize);
                    }));
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(sp =>
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "health_app.db");
                return new SQLiteAsyncConnection(dbPath);
            });

            builder.Services.AddApplicationServices();

            return builder.Build();
        }
    }
}
