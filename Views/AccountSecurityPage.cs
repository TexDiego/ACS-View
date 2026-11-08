using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

public sealed class AccountSecurityPage : ContentPage
{
    private readonly AccountCredentialsView form;
#if WINDOWS
    private readonly WindowsHelloSettingsView biometrics;
#else
    private readonly BiometricSettingsView biometrics;
#endif
    public AccountSecurityPage(IAuthService authService, IRememberedAccountService accounts, IDialogService dialogs)
    {
        Title = "Segurança da conta";
        Style = (Style)Microsoft.Maui.Controls.Application.Current!.Resources["PageBackground"];
        Shell.SetTabBarIsVisible(this, false);
        form = new AccountCredentialsView(this, authService, AccountFormKind.Security,
            async () =>
            {
                if (await authService.GetCurrentUserAsync() is not null)
                    await Shell.Current.GoToAsync("..");
                else if (Microsoft.Maui.Controls.Application.Current is App app)
                    await app.ResetToLoginShellAsync();
            });
#if WINDOWS
        biometrics = new WindowsHelloSettingsView(authService, accounts, dialogs);
#else
        biometrics = new BiometricSettingsView(authService);
#endif
        form.RecoveryCodeShown += (_, _) => biometrics.IsVisible = false;
        Content = new ScrollView { Content = new VerticalStackLayout { Children = { biometrics, form } } };
        form.UseParentScrolling();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await biometrics.RefreshAsync();
        InputFocusGuard.ClearTextInputFocus(this);
    }
    protected override bool OnBackButtonPressed() => form.ShowingCode || base.OnBackButtonPressed();
}
