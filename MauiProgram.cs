using ACS_View.Infrastructure.DependencyInjection;
using ACS_View.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
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
