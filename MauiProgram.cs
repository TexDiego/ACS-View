using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
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

            // Singletons - não dependem de dados variáveis por página
            builder.Services.AddSingleton<NoteService>();

            builder.Services.LoadInterfaces();

            // ViewModels - Transient
            builder.Services.AddTransient<AddRegister>();
            builder.Services.AddTransient<AddRegisterViewModel>();
            builder.Services.AddTransient<RegistersViewModel>();
            builder.Services.AddTransient<HousesPageViewModel>();
            builder.Services.AddTransient<OverallViewModel>();

            return builder.Build();
        }

        private static IServiceCollection LoadInterfaces(this IServiceCollection service)
        {
            // Singleton
            service.AddSingleton<IDatabaseService, DatabaseService>();
            service.AddSingleton<IHouseService, HouseService>();
            service.AddSingleton<IHealthRecordService, HealthRecordService>();
            service.AddSingleton<INavigationService, NavigationService>();
            service.AddSingleton<IVisitsService, VisitsService>();
            service.AddSingleton<IDashboardService, DashboardService>();

            // Transient
            service.AddTransient<IPatientConditionsService, PatientConditionService>();
            service.AddTransient<IVaccineService, VaccineService>();
            service.AddTransient<IFamilyService, FamilyService>();
            service.AddTransient<IFamilyManager, FamilyManager>();
            service.AddTransient<IHealthRecordFilterService, HealthRecordFilterService>();
            service.AddTransient<IUserDialogService, UserDialogService>();
            service.AddTransient<IPersonsInfoService, PersonsInfoService>();
            service.AddTransient<IRegisterValidator, RegisterValidator>();
            service.AddTransient<IRegisterFactory, RegisterFactory>();
            service.AddTransient<INoteService, NoteService>();
            service.AddTransient<IPatientService, PatientService>();

            return service;
        }
    }
}