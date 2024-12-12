using ACS_View.MVVM.Models.Services;
using CommunityToolkit.Maui.Views;

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
            try
            {
                AppServiceLocator.Instance.DatabaseService.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, false, "", true, "Voltar"));
            }            
        }
    }
}
