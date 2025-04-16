using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class VaccinesPage : ContentPage
{
    VaccinesPageViewModel viewModel;
    private readonly DatabaseService _databaseService;
    private readonly HealthRecord _healthRecord;

    public VaccinesPage(HealthRecord healthRecord, DatabaseService databaseService)
	{
		InitializeComponent();
        
        _healthRecord = healthRecord ?? throw new ArgumentNullException(nameof(healthRecord));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            viewModel = new VaccinesPageViewModel(new HealthRecordService(_databaseService), _healthRecord.SusNumber);
            BindingContext = viewModel;
        }
        catch (Exception ex)
        {
            Application.Current.MainPage.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private async void Btn_GoBack_Clicked(object sender, EventArgs e)
    {
		try
		{
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            Application.Current.MainPage.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}