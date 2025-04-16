using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class ForgotPassword : ContentPage
{
    private readonly DatabaseService databaseService;

	public ForgotPassword()
	{
		InitializeComponent();
        databaseService = new();
        BindingContext = new ForgotPasswordViewModel(databaseService);
	}

    private void ConfirmButton_Clicked(object sender, EventArgs e)
    {

    }

    private async void GoBackButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}