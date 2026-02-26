using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

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

        ViewPasswordButton.BackgroundColor = Colors.Gray;
        PasswordEntry.IsPassword = true;
    }

    private async void GoBackButton_Clicked(object sender, EventArgs e)
    {
		try
		{
			await Navigation.PopAsync();
		}
		catch (Exception ex)
		{
            this.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private void ViewPasswordButton_Clicked(object sender, EventArgs e)
    {
		bool isPasswordVisible = PasswordEntry.IsPassword;

		ViewPasswordButton.BackgroundColor = isPasswordVisible ? Colors.Transparent : Colors.Gray;
		PasswordEntry.IsPassword = !isPasswordVisible;

        Console.WriteLine(isPasswordVisible);
    }
}