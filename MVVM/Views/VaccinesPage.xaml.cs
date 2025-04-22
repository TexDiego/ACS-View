using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class VaccinesPage : ContentPage
{
    VaccinesPageViewModel viewModel;
    private readonly DatabaseService _databaseService;
    private HealthRecord _healthRecord;
    private readonly Vaccines _vaccines;
    private readonly VaccineService _vaccineService;
    private readonly HealthRecordService _healthRecordService;

    private string susNumber;

    public VaccinesPage(Vaccines vaccines, VaccineService vaccineService, DatabaseService databaseService)
	{
		InitializeComponent();
        
        _vaccines = vaccines ?? throw new ArgumentNullException(nameof(vaccines));
        _vaccineService = vaccineService ?? throw new ArgumentNullException(nameof(vaccineService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _healthRecordService = new HealthRecordService(_databaseService);
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _healthRecord = await _healthRecordService.GetRecordBySusAsync(_vaccines.SusNumber);
            susNumber = _healthRecord.SusNumber;

            viewModel = new VaccinesPageViewModel(_healthRecordService, _vaccineService, susNumber);
            BindingContext = viewModel;
        }
        catch (Exception ex)
        {
            Application.Current.MainPage.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine(ex.InnerException);
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