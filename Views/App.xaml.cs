using ACS_View.Domain.Interfaces;

namespace ACS_View.Views
{
    public partial class App : Application
    {
        private readonly IDatabaseService db;
        public static IServiceProvider ServiceProvider { get; private set; }

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
            return new Window(new AppShell(db));
        }

        private async Task InitializeDatabase()
        {
            await db.InitializeAsync();

            var seeder = ServiceProvider.GetRequiredService<ICidSeeder>();
            await seeder.SeedAsync();
        }
    }
}