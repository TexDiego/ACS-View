namespace ACS_View.MVVM.Views
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            SQLitePCL.Batteries_V2.Init();

            MainPage = new NavigationPage(new LoginPage());
        }
    }
}