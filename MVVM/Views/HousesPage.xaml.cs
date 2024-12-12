namespace ACS_View.MVVM.Views;

public partial class HousesPage : ContentPage
{
	public HousesPage()
	{
		InitializeComponent();
	}

    private void Btn_Voltar_Clicked(object sender, EventArgs e)
    {
		Navigation.PopAsync();
    }

    private async void Btn_AddHouse_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddHouse());
    }
}