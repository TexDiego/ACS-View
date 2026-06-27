using ACS_View.Infrastructure.DependencyInjection;
using ACS_View.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using SQLite;

namespace ACS_View
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
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
