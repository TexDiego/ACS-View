using ACS_View.MVVM.Models.Interfaces;

namespace ACS_View.MVVM.Views
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            SQLitePCL.Batteries_V2.Init();
            ServiceProvider = serviceProvider;
            MainPage = new NavigationPage(new LoginPage());
        }
    }
}