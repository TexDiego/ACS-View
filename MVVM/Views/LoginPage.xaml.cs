using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModel;

namespace ACS_View.MVVM.Views;

public partial class LoginPage : ContentPage
{
	DatabaseService _databaseService;
	HealthRecordService _healthRecordService;
	OverallViewModel _overallViewModel;
	AddRegisterViewModel _addRegisterViewModel;
	public LoginPage()
	{
		InitializeComponent();

		_databaseService = new DatabaseService();
		_healthRecordService = new HealthRecordService(_databaseService);
        _overallViewModel = new OverallViewModel(_healthRecordService);
		_addRegisterViewModel = new AddRegisterViewModel();
    }

    private async void Btn_Login_Clicked(object sender, EventArgs e)
    {
		try
		{
		await Navigation.PushAsync(new OverallView(_databaseService, _healthRecordService, _overallViewModel, _addRegisterViewModel));
		}
		catch (Exception ex)
		{
			await DisplayAlert("Erro", ex.Message, "Voltar");
		}
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
    }
}