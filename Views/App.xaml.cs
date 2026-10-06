using ACS_View.Application.Interfaces;
using ACS_View.Application.State;

namespace ACS_View.Views
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IAppStartupService appStartupService;
        private readonly TransientInputLifecycle inputLifecycle = new();
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
            window.Stopped += (_, _) => inputLifecycle.OnStopped();
            window.Resumed += (_, _) =>
            {
                if (!inputLifecycle.ConsumeReset())
                {
                    return;
                }

                // Discard every retained page, including forms and modal inputs.
                window.Page = CreateShell(isAuthenticated: false);
                _ = RestoreSessionAsync(window);
            };
            _ = RestoreSessionAsync(window);

            return window;
        }

        public IDisposable BeginExternalInteraction() => inputLifecycle.BeginExternalInteraction();

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
            var initialPage = window.Page;
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
                    // A login, logout or another resume may have replaced this Shell.
                    if (!ReferenceEquals(window.Page, initialPage))
                    {
                        return;
                    }

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
