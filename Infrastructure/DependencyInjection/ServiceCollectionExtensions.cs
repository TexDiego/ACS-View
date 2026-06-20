using ACS_View.Domain.Interfaces;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.UseCases;
using ACS_View.UseCases.Services;
using ACS_View.ViewModels;
using ACS_View.Views;

namespace ACS_View.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddDomainServices();
        services.AddViewModels();
        services.AddViews();

        return services;
    }

    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IHouseService, HouseService>();
        services.AddSingleton<IVisitsService, VisitsService>();
        services.AddSingleton<IDashboardMetricsService, DashboardMetricsService>();
        services.AddSingleton<ISQLiteConditionsRepository, SQLiteConditionsRepository>();
        services.AddSingleton<ICidRepository, SQLiteCidRepository>();
        services.AddSingleton<ICidSeeder, CidSeeder>();
        services.AddSingleton<IPatientConditionSeeder, PatientConditionsSeeder>();

        services.AddTransient<IVaccineService, VaccineService>();
        services.AddTransient<IFamilyService, FamilyService>();
        services.AddTransient<IFamilyManager, FamilyManager>();
        services.AddTransient<IPersonsInfoService, PersonsInfoService>();
        services.AddTransient<INoteService, NoteService>();
        services.AddTransient<IPatientService, PatientService>();
        services.AddTransient<IPatientImportService, PatientImportService>();
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
        services.AddTransient<FamiliesPage>();
        services.AddTransient<Profile>();
        services.AddTransient<VaccinesPage>();

        services.AddTransient<HousesPage>();
        services.AddTransient<NotesPage>();
        services.AddTransient<Registers>();
        services.AddTransient<OverallView>();
        services.AddTransient<GeneralMetrics>();
        services.AddTransient<HealthMetrics>();

        return services;
    }
}
