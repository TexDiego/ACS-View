using ACS_View.MVVM.Models.Interfaces;

namespace ACS_View.MVVM.Views
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
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell(db));
        }
    }
}