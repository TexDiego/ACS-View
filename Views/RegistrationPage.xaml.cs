using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class RegistrationPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly RegistrationViewModel viewModel;

    public RegistrationPage(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
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

            await DisplayAlert("Sucesso", "Usuário cadastrado com sucesso.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }
}
