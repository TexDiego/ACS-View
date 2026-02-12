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

            return builder.Build();
        }

        private static IServiceCollection LoadInterfaces(this IServiceCollection service)
        {
            service.AddSingleton<IDatabaseService, DatabaseService>();
            service.AddSingleton<IHealthSummaryService, HealthSummaryService>();
            service.AddSingleton<IVaccineService, VaccineService>();
            service.AddSingleton<IHouseService, HouseService>();
            service.AddSingleton<IHealthRecordService, HealthRecordService>();
            service.AddSingleton<IFamilyService, FamilyService>();
            service.AddSingleton<IFamilyManager, FamilyManager>();
            service.AddSingleton<IVaccineService, VaccineService>();
            service.AddSingleton<IHealthRecordFilterService, HealthRecordFilterService>();
            service.AddSingleton<IUserDialogService, UserDialogService>();
            service.AddSingleton<IPersonsInfoService, PersonsInfoService>();
            service.AddSingleton<IRegisterValidator, RegisterValidator>();
            service.AddSingleton<IRegisterFactory, RegisterFactory>();
            service.AddSingleton<INoteService, NoteService>();

            return service;
        }
    }
}