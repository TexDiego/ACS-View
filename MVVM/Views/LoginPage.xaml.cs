using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModel;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class LoginPage : ContentPage
{
	DatabaseService _databaseService;
	HealthRecordService _healthRecordService;
	OverallViewModel _overallViewModel;
	AddRegisterViewModel _addRegisterViewModel;
	HouseService _houseService;
	HousesPageViewModel _housesPageViewModel;
	FamilyService _familyService;

	public LoginPage()
	{
		InitializeComponent();

		_databaseService = new DatabaseService();
		_healthRecordService = new HealthRecordService(_databaseService);
        _overallViewModel = new OverallViewModel(_healthRecordService);
		_addRegisterViewModel = new AddRegisterViewModel();
		_houseService = new HouseService(_databaseService);
		_housesPageViewModel = new HousesPageViewModel(_houseService);
		_familyService = new FamilyService(_databaseService);
	}

    private async void Btn_Login_Clicked(object sender, EventArgs e)
    {
		try
		{
			await Navigation.PushAsync(new OverallView(_databaseService, _healthRecordService, _overallViewModel, _addRegisterViewModel));
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