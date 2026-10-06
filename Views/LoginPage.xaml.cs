using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;
    private bool signingIn;

    public LoginPage(IAuthService authService, IDialogService dialogService)
    {
        InitializeComponent();
        _authService = authService;
        _dialogService = dialogService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        InputFocusGuard.ClearTextInputFocus(this);
        var status = await _authService.GetBiometricStatusAsync();
        BiometricLoginButton.IsVisible = status.Enabled;
    }

    protected override void OnDisappearing()
    {
        InputFocusGuard.ClearTextInputFocus(this);
        base.OnDisappearing();
    }

    private async void Btn_Login_Clicked(object sender, EventArgs e)
    {
        if (signingIn) return;
        signingIn = true;
        if (sender is Button button) button.IsEnabled = false;
        try
        {
            await _authService.LoginAsync(UsernameEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);

            if (Microsoft.Maui.Controls.Application.Current is App app)
            {
                await app.ResetToAuthenticatedShellAsync();
                PasswordEntry.Text = string.Empty;
                var user = await _authService.GetCurrentUserAsync();
                if (string.IsNullOrEmpty(user?.RecoveryCodeHash))
                    await Shell.Current.GoToAsync("accountsecurity");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", ex.Message, "Voltar");
        }
        finally
        {
            signingIn = false;
            if (sender is Button loginButton) loginButton.IsEnabled = true;
        }
    }

    private async void RegisterUser_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("registration");
    }

    private async void BiometricLogin_Clicked(object sender, EventArgs e)
    {
        if (signingIn) return;
        signingIn = true;
        Btn_Login.IsEnabled = BiometricLoginButton.IsEnabled = false;
        using var interaction = (Microsoft.Maui.Controls.Application.Current as App)?.BeginExternalInteraction();
        try
        {
            await _authService.LoginWithBiometricsAsync();
            PasswordEntry.Text = string.Empty;
            if (Microsoft.Maui.Controls.Application.Current is App app)
                await app.ResetToAuthenticatedShellAsync();
        }
        catch (OperationCanceledException) { PasswordEntry.Focus(); }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Biometria", ex is InvalidOperationException ? ex.Message : "Entre com a senha da conta.", "OK");
        }
        finally
        {
            signingIn = false;
            Btn_Login.IsEnabled = BiometricLoginButton.IsEnabled = true;
            BiometricLoginButton.IsVisible = (await _authService.GetBiometricStatusAsync()).Enabled;
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("forgotpassword");
    }
}
