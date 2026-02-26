using ACS_View.Domain.Interfaces;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.UseCases;
using ACS_View.UseCases.Services;
using ACS_View.ViewModels;
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

            // Singletons - não dependem de dados variáveis por página
            builder.Services.AddSingleton<NoteService>();

            builder.Services.LoadInterfaces();

            // ViewModels - Transient
            builder.Services.AddTransient<AddRegister>();
            builder.Services.AddTransient<OverallView>();
            builder.Services.AddTransient<AddRegisterViewModel>();
            builder.Services.AddTransient<RegistersViewModel>();
            builder.Services.AddTransient<HousesPageViewModel>();
            builder.Services.AddTransient<OverallViewModel>();

            builder.Services.AddSingleton(sp =>
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "health_app.db");
                return new SQLiteAsyncConnection(dbPath);
            });

            builder.Services.AddSingleton<ICidRepository, SQLiteCidRepository>();
            builder.Services.AddSingleton<ICidSeeder, CidSeeder>();

            return builder.Build();
        }

        private static IServiceCollection LoadInterfaces(this IServiceCollection service)
        {
            // Singleton
            service.AddSingleton<IDatabaseService, DatabaseService>();
            service.AddSingleton<IHouseService, HouseService>();
            service.AddSingleton<IHealthRecordService, HealthRecordService>();
            service.AddSingleton<IVisitsService, VisitsService>();
            service.AddSingleton<IDashboardMetricsService, DashboardMetricsService>();

            // Transient
            service.AddTransient<IVaccineService, VaccineService>();
            service.AddTransient<IFamilyService, FamilyService>();
            service.AddTransient<IFamilyManager, FamilyManager>();
            service.AddTransient<IUserDialogService, UserDialogService>();
            service.AddTransient<IPersonsInfoService, PersonsInfoService>();
            service.AddTransient<INoteService, NoteService>();
            service.AddTransient<IPatientService, PatientService>();

            return service;
        }
    }
}