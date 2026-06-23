using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;

    public LoginPage(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void Btn_Login_Clicked(object sender, EventArgs e)
    {
        try
        {
            await _authService.LoginAsync(UsernameEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);

            if (Microsoft.Maui.Controls.Application.Current is App app)
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
        await Shell.Current.GoToAsync("registration");
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("forgotpassword");
    }
}
