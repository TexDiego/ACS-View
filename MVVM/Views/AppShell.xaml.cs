using ACS_View.MVVM.Models.Interfaces;

namespace ACS_View.MVVM.Views;

public partial class AppShell : Shell
{
    private readonly IDatabaseService databaseService;
    public AppShell(IDatabaseService db)
    {
        databaseService = db;
        InitializeComponent();
        RegisterRoutes();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await InitializeDb();

        // Verifica se há um token/flag de autenticação salvo; ajuste a chave conforme sua implementação.
        var token = Preferences.Get("AuthToken", string.Empty);

        if (string.IsNullOrEmpty(token))
            await Shell.Current.GoToAsync("login");
        else
            await Shell.Current.GoToAsync("//overview");
    }

    private static void RegisterRoutes()
    {
        // Registrar apenas rotas que não estão declaradas no AppShell.xaml (evita duplicação)
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("families", typeof(FamiliesPage));
        Routing.RegisterRoute("addregister", typeof(AddRegister));
        Routing.RegisterRoute("addfamily", typeof(AddFamilyPage));
        Routing.RegisterRoute("addhouse", typeof(AddHouse));
    }

    private async Task InitializeDb()
    {
        if (databaseService.Connection is null)
            await databaseService.InitializeAsync();
    }
}