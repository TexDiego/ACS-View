using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

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

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<HealthRecordService>();
            builder.Services.AddSingleton<VaccineService>();
            builder.Services.AddSingleton<NoteService>();
            builder.Services.AddSingleton<HouseService>();
            builder.Services.AddSingleton<FamilyService>();

            return builder.Build();
        }
    }
}