using ACS_View.Application.Interfaces;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases;
using ACS_View.UseCases.Services;
using ACS_View.ViewModels;
using ACS_View.Views;

namespace ACS_View.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        BaseViewModel.ConfigureInfrastructure(
            new ShellDialogService(),
            new ShellNavigationService(),
            new MauiMainThreadDispatcher());

        services.AddDomainServices();
        services.AddViewModels();
        services.AddViews();

        return services;
    }

    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IHouseService, HouseService>();
        services.AddSingleton<IVisitsService, VisitsService>();
        services.AddSingleton<IDashboardMetricsService, DashboardMetricsService>();
        services.AddSingleton<IDashboardMetricPreferencesService, DashboardMetricPreferencesService>();
        services.AddSingleton<ISQLiteConditionsRepository, SQLiteConditionsRepository>();
        services.AddSingleton<ICidRepository, SQLiteCidRepository>();
        services.AddSingleton<ICidSeeder, CidSeeder>();
        services.AddSingleton<IPatientConditionSeeder, PatientConditionsSeeder>();
        services.AddSingleton<IPersonsInfoPopupService, PersonsInfoPopupService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserDataCleanupService, UserDataCleanupService>();
        services.AddSingleton<IDialogService, ShellDialogService>();
        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IMainThreadDispatcher, MauiMainThreadDispatcher>();
        services.AddSingleton<IPopupService, PopupService>();
        services.AddSingleton<IHouseRepository, SQLiteHouseRepository>();
        services.AddSingleton<IPatientRepository, SQLitePatientRepository>();
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("https://viacep.com.br/"),
            Timeout = TimeSpan.FromSeconds(10)
        });
        services.AddSingleton<ICepService, ViaCepService>();

        services.AddTransient<IVaccineService, VaccineService>();
        services.AddTransient<IFamilyService, FamilyService>();
        services.AddTransient<IFamilyManager, FamilyManager>();
        services.AddTransient<IPersonsInfoService, PersonsInfoService>();
        services.AddTransient<INoteService, NoteService>();
        services.AddTransient<IPatientService, PatientService>();
        services.AddTransient<IPatientImportService, PatientImportService>();
        services.AddTransient<IHouseImportService, HouseImportService>();
        services.AddTransient<IPatientCidRepository, PatientCidService>();

        return services;
    }

    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<AddRegisterViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<AddHouseViewModel>();
        services.AddTransient<AllVisitsViewModel>();
        services.AddTransient<VisitsViewModel>();
        services.AddTransient<VaccinesPageViewModel>();
        services.AddTransient<NotesPageViewModel>();
        services.AddTransient<PersonsInfoViewModel>();
        services.AddTransient<Func<PersonsInfoViewModel>>(provider => provider.GetRequiredService<PersonsInfoViewModel>);
        services.AddTransient<ImportDataViewModel>();
        services.AddTransient<DataCleanupViewModel>();
        services.AddTransient<ForgotPasswordViewModel>();

        services.AddSingleton<RegistersViewModel>();
        services.AddSingleton<HousesPageViewModel>();
        services.AddSingleton<OverallViewModel>();

        return services;
    }

    private static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<AddRegister>();
        services.AddTransient<AddHouse>();
        services.AddTransient<AllVisits>();
        services.AddTransient<LoginPage>();
        services.AddTransient<RegistrationPage>();
        services.AddTransient<ForgotPassword>();
        services.AddTransient<FamiliesPage>();
        services.AddTransient<Profile>();
        services.AddTransient<VaccinesPage>();
        services.AddTransient<ImportDataPage>();
        services.AddTransient<DataCleanupPage>();

        services.AddTransient<HousesPage>();
        services.AddTransient<NotesPage>();
        services.AddTransient<Registers>();
        services.AddTransient<OverallView>();
        services.AddTransient<GeneralMetrics>();
        services.AddTransient<HealthMetrics>();

        return services;
    }
}
