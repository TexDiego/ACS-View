using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class DisplaySheetPopUp : Popup
{
    int _choice;

    public DisplaySheetPopUp(string title, string item1, string item2, string cancel, int choice)
    {
        InitializeComponent();

        Lbl_title.Text = title;
        Btn_item1.Text = item1;
        Btn_item2.Text = item2;
        Btn_Voltar.Text = cancel;
        _choice = choice;
    }

    private void Btn_item1_Clicked(object sender, EventArgs e)
    {
        if (_choice == 1)
        {
            this.Close("Nome");
            return;
        }
        this.Close("Crescente");
    }

    private void Btn_Voltar_Clicked(object sender, EventArgs e)
    {
        this.Close();
    }

    private void Btn_item2_Clicked(object sender, EventArgs e)
    {
        if (_choice == 1)
        {
            this.Close("Idade");
            return;
        }
        this.Close("Decrescente");
    }
}