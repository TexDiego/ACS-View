using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class ForgotPassword : ContentPage
{
    private readonly IDatabaseService databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();

	public ForgotPassword()
	{
		InitializeComponent();
        BindingContext = new ForgotPasswordViewModel();
	}

    private void ConfirmButton_Clicked(object sender, EventArgs e)
    {

    }

    private async void GoBackButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}