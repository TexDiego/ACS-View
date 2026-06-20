using ACS_View.Domain.Interfaces;

namespace ACS_View.Views
{
    public partial class App : Application
    {
        private readonly IDatabaseService db;
        public IServiceProvider ServiceProvider { get; private set; }
        public static IServiceProvider StaticServiceProvider { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            SQLitePCL.Batteries_V2.Init();
            ServiceProvider = serviceProvider;
            StaticServiceProvider = serviceProvider;
            db = serviceProvider.GetRequiredService<IDatabaseService>();

            MainThread.BeginInvokeOnMainThread(async () => await InitializeDatabase());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = CreateShell();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var route = string.IsNullOrEmpty(Preferences.Get("AuthToken", string.Empty))
                    ? "login"
                    : "//overallview";

                await shell.GoToAsync(route);
            });

            return new Window(shell);
        }

        public Task ResetToAuthenticatedShellAsync()
        {
            return ResetShellAsync("//overallview");
        }

        public Task ResetToLoginShellAsync()
        {
            return ResetShellAsync("login");
        }

        private AppShell CreateShell()
        {
            return new AppShell(db, ServiceProvider);
        }

        private Task ResetShellAsync(string route)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var shell = CreateShell();
                var window = Windows.FirstOrDefault();

                if (window is null)
                {
                    return;
                }

                window.Page = shell;
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
