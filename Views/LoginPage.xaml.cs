namespace ACS_View.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void Btn_Login_Clicked(object sender, EventArgs e)
    {
        try
        {
            Preferences.Set("AuthToken", "User");

            if (Application.Current is App app)
            {
                await app.ResetToAuthenticatedShellAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "Voltar");
        }
    }

    private async void RegisterUser_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistrationPage());
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ForgotPassword());
    }
}
