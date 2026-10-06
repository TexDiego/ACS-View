using ACS_View.Application.Interfaces;

namespace ACS_View.Views;

public partial class ForgotPassword : ContentPage
{
    private readonly AccountCredentialsView form;
    public ForgotPassword(IAuthService authService)
    {
        InitializeComponent();
        Content = form = new AccountCredentialsView(this, authService, AccountFormKind.Recovery,
            () => Shell.Current.GoToAsync("//login"));
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
