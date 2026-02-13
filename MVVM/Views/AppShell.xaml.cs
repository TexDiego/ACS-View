namespace ACS_View.MVVM.Views;

public partial class AppShell : Shell
{
	public AppShell()
	{
        RegisterRoutes();
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.Current.GoToAsync("login");
    }

    private static void RegisterRoutes()
    {
        // Registrar apenas rotas que não estão declaradas no AppShell.xaml (evita duplicação)
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("families", typeof(FamiliesPage));
        Routing.RegisterRoute("addregister", typeof(AddRegister));
    }
}