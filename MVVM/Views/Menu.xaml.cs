using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class Menu : Popup
{
	public Menu()
	{
		InitializeComponent();
	}

    private void Btn_AddRegister_Clicked(object sender, EventArgs e)
    {
        this.Close("1");
    }

    private void Btn_AddHouses_Clicked(object sender, EventArgs e)
    {
        this.Close("2");
    }

    private void Btn_Notes_Clicked(object sender, EventArgs e)
    {
        this.Close("3");
    }

    private void Btn_Exit_Clicked(object sender, EventArgs e)
    {
        this.Close("4");
    }

    private void Btn_Cancel_Clicked(object sender, EventArgs e)
    {
        this.Close();
    }
}