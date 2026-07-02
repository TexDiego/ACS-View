using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class RegistrationPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;
    private readonly RegistrationViewModel viewModel;

    public RegistrationPage(IAuthService authService, IDialogService dialogService)
    {
        InitializeComponent();
        _authService = authService;
        _dialogService = dialogService;
        BindingContext = viewModel = new();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ViewPasswordButton.BackgroundColor = ThemeColors.ControlPressed;
        PasswordEntry.IsPassword = true;
    }

    private void ViewPasswordButton_Clicked(object sender, EventArgs e)
    {
        bool isPasswordVisible = PasswordEntry.IsPassword;

        ViewPasswordButton.BackgroundColor = isPasswordVisible ? Colors.Transparent : ThemeColors.ControlPressed;
        PasswordEntry.IsPassword = !isPasswordVisible;
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await _authService.RegisterAsync(
                UsernameEntry.Text ?? string.Empty,
                PasswordEntry.Text ?? string.Empty,
                SecurityQuestionPicker.SelectedItem?.ToString() ?? string.Empty,
                SecurityAnswerEntry.Text ?? string.Empty);

            await _dialogService.ShowAlertAsync("Sucesso", "Usuário cadastrado com sucesso.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", ex.Message, "OK");
        }
    }
}
