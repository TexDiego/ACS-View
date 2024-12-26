using CommunityToolkit.Maui.Views;

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

    private async void Button_Clicked(object sender, EventArgs e)
    {
        try
        {

        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}