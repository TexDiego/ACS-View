namespace ACS_View.Views;

public partial class ForgotPassword : ContentPage
{
    internal ForgotPassword()
	{
		InitializeComponent();
	}

    private void ConfirmButton_Clicked(object sender, EventArgs e)
    {

    }

    private async void GoBackButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}