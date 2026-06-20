using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class RegistrationPage : ContentPage
{
    private readonly RegistrationViewModel viewModel;

	public RegistrationPage()
	{
		InitializeComponent();
        viewModel = new();
        BindingContext = viewModel = new();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ViewPasswordButton.BackgroundColor = ThemeColors.ControlPressed;
        PasswordEntry.IsPassword = true;
    }

    private async void GoBackButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void ViewPasswordButton_Clicked(object sender, EventArgs e)
    {
		bool isPasswordVisible = PasswordEntry.IsPassword;

		ViewPasswordButton.BackgroundColor = isPasswordVisible ? Colors.Transparent : ThemeColors.ControlPressed;
		PasswordEntry.IsPassword = !isPasswordVisible;

        Console.WriteLine(isPasswordVisible);
    }
}
