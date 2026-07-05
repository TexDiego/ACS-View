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
        var dialogService = new ShellDialogService();
        var navigationService = new ShellNavigationService();
        var mainThreadDispatcher = new MauiMainThreadDispatcher();

        BaseViewModel.ConfigureInfrastructure(
            dialogService,
            navigationService,
            mainThreadDispatcher);

        services.AddSingleton<IDialogService>(dialogService);
        services.AddSingleton<INavigationService>(navigationService);
        services.AddSingleton<IMainThreadDispatcher>(mainThreadDispatcher);

        services.AddDomainServices();
        services.AddViewModels();
        services.AddViews();

        return services;
    }

    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IAppStartupService, AppStartupService>();
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
        services.AddSingleton<IPopupService, PopupService>();
        services.AddSingleton<IHouseRepository, SQLiteHouseRepository>();
        services.AddSingleton<IPatientRepository, SQLitePatientRepository>();
        services.AddSingleton<IPatientBolsaFamiliaRepository, SQLitePatientBolsaFamiliaRepository>();
        services.AddSingleton<IPatientInsulinDependencyRepository, SQLitePatientInsulinDependencyRepository>();
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("https://viacep.com.br/"),
            Timeout = TimeSpan.FromSeconds(10)
        });
        services.AddSingleton<ViaCepService>();
        services.AddSingleton<ICepService, CachedCepService>();

        services.AddTransient<IVaccineService, VaccineService>();
        services.AddTransient<IFamilyService, FamilyService>();
        services.AddTransient<IFamilyManager, FamilyManager>();
        services.AddTransient<IPersonsInfoService, PersonsInfoService>();
        services.AddTransient<INoteService, NoteService>();
        services.AddTransient<IPatientService, PatientService>();
        services.AddTransient<ISpreadsheetReader, SpreadsheetReader>();
        services.AddTransient<PatientFamilyLinkResolver>();
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
        services.AddTransient<VisitRecordsViewModel>();
        services.AddTransient<VisitSuggestionsViewModel>();
        services.AddTransient<VisitsViewModel>();
        services.AddTransient<VaccinesPageViewModel>();
        services.AddTransient<NotesPageViewModel>();
        services.AddTransient<BolsaFamiliaPageViewModel>();
        services.AddTransient<PersonsInfoViewModel>();
        services.AddTransient<Func<PersonsInfoViewModel>>(provider => provider.GetRequiredService<PersonsInfoViewModel>);
        services.AddTransient<ImportDataViewModel>();
        services.AddTransient<DataCleanupViewModel>();
        services.AddTransient<ForgotPasswordViewModel>();

        services.AddSingleton<RegistersViewModel>();
        services.AddSingleton<HousesPageViewModel>();
        services.AddSingleton<CIDViewViewModel>();

        services.AddTransient<OverallViewModel>();

        return services;
    }

    private static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<AddRegister>();
        services.AddTransient<AddHouse>();
        services.AddTransient<AllVisits>();
        services.AddTransient<VisitRecordsPage>();
        services.AddTransient<VisitSuggestionsPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<RegistrationPage>();
        services.AddTransient<ForgotPassword>();
        services.AddTransient<FamiliesPage>();
        services.AddTransient<BolsaFamiliaPage>();
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
        services.AddTransient<CIDView>();

        return services;
    }
}
