using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

public partial class AppShell : Shell
{
    private static bool routesRegistered;
    private readonly IDatabaseService _databaseService;
    private readonly IServiceProvider _serviceProvider;

    internal AppShell(IDatabaseService db, IServiceProvider serviceProvider)
    {
        _databaseService = db;
        _serviceProvider = serviceProvider;
        InitializeComponent();
        RegisterRoutes();
        RegistersPage.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<Registers>());
        HousesPageContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<HousesPage>());
        OverallViewContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<OverallView>());
        AllVisitsContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<AllVisits>());
        ProfileContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<Profile>());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeDb();
    }

    private void RegisterRoutes()
    {
        if (routesRegistered)
        {
            return;
        }

        routesRegistered = true;

        Routing.RegisterRoute("login", new ServiceProviderRouteFactory<LoginPage>(_serviceProvider));
        Routing.RegisterRoute("registration", new ServiceProviderRouteFactory<RegistrationPage>(_serviceProvider));
        Routing.RegisterRoute("forgotpassword", new ServiceProviderRouteFactory<ForgotPassword>(_serviceProvider));
        Routing.RegisterRoute("families", new ServiceProviderRouteFactory<FamiliesPage>(_serviceProvider));
        Routing.RegisterRoute("addregister", new ServiceProviderRouteFactory<AddRegister>(_serviceProvider));
        Routing.RegisterRoute("addhouse", new ServiceProviderRouteFactory<AddHouse>(_serviceProvider));
        Routing.RegisterRoute("notes", new ServiceProviderRouteFactory<NotesPage>(_serviceProvider));
        Routing.RegisterRoute("vaccines", new ServiceProviderRouteFactory<VaccinesPage>(_serviceProvider));
        Routing.RegisterRoute("importdata", new ServiceProviderRouteFactory<ImportDataPage>(_serviceProvider));
    }

    private async Task InitializeDb()
    {
        if (_databaseService.Connection is null)
        {
            await _databaseService.InitializeAsync();
        }
    }
}
