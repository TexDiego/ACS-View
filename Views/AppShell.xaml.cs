using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

public partial class AppShell : Shell
{
    private const string LightStatusBarColorResourceKey = "Primary";
    private const string DarkStatusBarColorResourceKey = "DarkPrimaryLight";
    private static bool routesRegistered;
    private readonly IServiceProvider _serviceProvider;
    private bool isThemeChangeHandlerRegistered;

    internal AppShell(IServiceProvider serviceProvider, bool isAuthenticated)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        RegisterRoutes();
        ApplyStatusBarTheme();

        if (isAuthenticated)
        {
            Items.Remove(LoginContent);
            RegistersPage.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<Registers>());
            HousesPageContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<HousesPage>());
            OverallViewContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<OverallView>());
            AllVisitsContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<AllVisits>());
            ProfileContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<Profile>());
            return;
        }

        Items.Remove(MainTabBar);
        LoginContent.ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<LoginPage>());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyStatusBarTheme();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        RegisterThemeChangeHandler();
        ApplyStatusBarTheme();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        UnregisterThemeChangeHandler();
    }

    private void RegisterThemeChangeHandler()
    {
        if (isThemeChangeHandlerRegistered || Microsoft.Maui.Controls.Application.Current is null)
        {
            return;
        }

        Microsoft.Maui.Controls.Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        isThemeChangeHandlerRegistered = true;
    }

    private void UnregisterThemeChangeHandler()
    {
        if (!isThemeChangeHandlerRegistered || Microsoft.Maui.Controls.Application.Current is null)
        {
            return;
        }

        Microsoft.Maui.Controls.Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        isThemeChangeHandlerRegistered = false;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(ApplyStatusBarTheme);
    }

    private void ApplyStatusBarTheme()
    {
        var theme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme ?? AppTheme.Light;
        var colorKey = theme == AppTheme.Dark
            ? DarkStatusBarColorResourceKey
            : LightStatusBarColorResourceKey;

        StatusBarThemeBehavior.StatusBarColor = GetThemeColor(colorKey);
    }

    private static Color GetThemeColor(string resourceKey)
    {
        if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(resourceKey, out var appResource) == true &&
            appResource is Color appColor)
        {
            return appColor;
        }

        return Colors.Transparent;
    }

    private void RegisterRoutes()
    {
        if (routesRegistered)
        {
            return;
        }

        routesRegistered = true;

        Routing.RegisterRoute("registration", new ServiceProviderRouteFactory<RegistrationPage>(_serviceProvider));
        Routing.RegisterRoute("forgotpassword", new ServiceProviderRouteFactory<ForgotPassword>(_serviceProvider));
        Routing.RegisterRoute("families", new ServiceProviderRouteFactory<FamiliesPage>(_serviceProvider));
        Routing.RegisterRoute("addregister", new ServiceProviderRouteFactory<AddRegister>(_serviceProvider));
        Routing.RegisterRoute("addhouse", new ServiceProviderRouteFactory<AddHouse>(_serviceProvider));
        Routing.RegisterRoute("notes", new ServiceProviderRouteFactory<NotesPage>(_serviceProvider));
        Routing.RegisterRoute("vaccines", new ServiceProviderRouteFactory<VaccinesPage>(_serviceProvider));
        Routing.RegisterRoute("importdata", new ServiceProviderRouteFactory<ImportDataPage>(_serviceProvider));
        Routing.RegisterRoute("datacleanup", new ServiceProviderRouteFactory<DataCleanupPage>(_serviceProvider));
        Routing.RegisterRoute("cids", new ServiceProviderRouteFactory<CIDView>(_serviceProvider));
    }
}
