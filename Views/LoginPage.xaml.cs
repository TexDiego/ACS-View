using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;

    public LoginPage(IAuthService authService, IDialogService dialogService)
    {
        InitializeComponent();
        _authService = authService;
        _dialogService = dialogService;
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
            await _dialogService.ShowAlertAsync("Erro", ex.Message, "Voltar");
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
