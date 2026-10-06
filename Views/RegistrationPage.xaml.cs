using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

public partial class RegistrationPage : ContentPage
{
    private readonly AccountCredentialsView form;
    public RegistrationPage(IAuthService authService, IDialogService dialogService)
    {
        InitializeComponent();
        Content = form = new AccountCredentialsView(this, authService, AccountFormKind.Registration,
            () => Shell.Current.GoToAsync(".."));
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        InputFocusGuard.ClearTextInputFocus(this);
    }
    protected override void OnDisappearing()
    {
        InputFocusGuard.ClearTextInputFocus(this);
        base.OnDisappearing();
    }
    protected override bool OnBackButtonPressed() => form.ShowingCode || base.OnBackButtonPressed();
}
