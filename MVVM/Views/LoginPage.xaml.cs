using ACS_View.MVVM.Models.Services;
using CommunityToolkit.Maui.Views;
using System.Reflection.Metadata;

namespace ACS_View.MVVM.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void Btn_Login_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new OverallView());
        }
        catch (Exception ex)
        {
            this.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private async void RegisterUser_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new RegistrationPage());
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new ForgotPassword());
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}