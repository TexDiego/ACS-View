using ACS_View.MVVM.Models.Services;

namespace ACS_View.MVVM.Views
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new LoginPage());
        }

        protected override async void OnStart()
        {
            base.OnStart();

            // Inicializa o banco de dados
            await AppServiceLocator.Instance.DatabaseService.InitializeDatabaseAsync();
        }
    }
}
