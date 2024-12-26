using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class DisplayPopUp : Popup
{
    public DisplayPopUp(string title, string message, bool cancelButton, string cancel, bool acceptButton, string accept)
    {
        InitializeComponent();

        Lbl_title.Text = title;
        Lbl_message.Text = message;
        Btn_Voltar.IsVisible = cancelButton;
        Btn_Voltar.Text = cancel;
        Btn_Accept.IsVisible = acceptButton;
        Btn_Accept.Text = accept;
    }

    private void Btn_Voltar_Clicked(object sender, EventArgs e)
    {
        this.Close(false);
    }

    private void Btn_Accept_Clicked(object sender, EventArgs e)
    {
        this.Close(true);
    }
}