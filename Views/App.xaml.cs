using ACS_View.Application.Interfaces;

namespace ACS_View.Views
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IDatabaseService db;
        public IServiceProvider ServiceProvider { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            SQLitePCL.Batteries_V2.Init();
            ServiceProvider = serviceProvider;
            db = serviceProvider.GetRequiredService<IDatabaseService>();

            MainThread.BeginInvokeOnMainThread(async () => await InitializeDatabase());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = CreateShell(isAuthenticated: false);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await db.InitializeAsync();
                var authService = ServiceProvider.GetRequiredService<IAuthService>();
                if (!await authService.IsAuthenticatedAsync())
                {
                    return;
                }

                var window = Windows.FirstOrDefault();
                if (window is null)
                {
                    return;
                }

                var authenticatedShell = CreateShell(isAuthenticated: true);
                window.Page = authenticatedShell;
                await authenticatedShell.GoToAsync("//overallview");
            });

            return new Window(shell);
        }

        public Task ResetToAuthenticatedShellAsync()
        {
            return ResetShellAsync("//overallview");
        }

        public Task ResetToLoginShellAsync()
        {
            return ResetShellAsync("//login");
        }

        private AppShell CreateShell(bool isAuthenticated)
        {
            return new AppShell(db, ServiceProvider, isAuthenticated);
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

        private async Task InitializeDatabase()
        {
            await db.InitializeAsync();

            ICidSeeder cidSeeder = ServiceProvider.GetRequiredService<ICidSeeder>();
            IPatientConditionSeeder patientConditionSeeder = ServiceProvider.GetRequiredService<IPatientConditionSeeder>();

            await cidSeeder.SeedAsync();
            await patientConditionSeeder.SeedAsync();
        }
    }
}
