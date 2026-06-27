using ACS_View.Application.Interfaces;

namespace ACS_View.Views
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IAppStartupService appStartupService;
        public IServiceProvider ServiceProvider { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            SQLitePCL.Batteries_V2.Init();
            ServiceProvider = serviceProvider;
            appStartupService = serviceProvider.GetRequiredService<IAppStartupService>();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = CreateShell(isAuthenticated: false);
            var window = new Window(shell);
            _ = RestoreSessionAsync(window);

            return window;
        }

        public async Task ResetToAuthenticatedShellAsync()
        {
            await appStartupService.InitializeAsync();
            await ResetShellAsync("//overallview");
        }

        public Task ResetToLoginShellAsync()
        {
            return ResetShellAsync("//login");
        }

        private AppShell CreateShell(bool isAuthenticated)
        {
            return new AppShell(ServiceProvider, isAuthenticated);
        }

        private Task ResetShellAsync(string route)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var shell = CreateShell(route != "//login");
                var window = Windows.FirstOrDefault();

                if (window is null)
                {
                    return;
                }

                window.Page = shell;
                if (route == "//login")
                {
                    return;
                }

                await shell.GoToAsync(route);
            });
        }

        private async Task RestoreSessionAsync(Window window)
        {
            try
            {
                await appStartupService.InitializeAsync();

                var authService = ServiceProvider.GetRequiredService<IAuthService>();
                if (!await authService.IsAuthenticatedAsync())
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var authenticatedShell = CreateShell(isAuthenticated: true);
                    window.Page = authenticatedShell;
                    await authenticatedShell.GoToAsync("//overallview");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Nao foi possivel restaurar sessao inicial: {ex.Message}");
            }
        }
    }
}
