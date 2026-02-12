using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class VaccinesPage : ContentPage
{
    VaccinesPageViewModel viewModel = new();
    private HealthRecord _healthRecord;
    private readonly Vaccines _vaccines;
    private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();

    private string susNumber;
    private bool _isInitialized = false;


    public VaccinesPage(Vaccines vaccines)
	{
        InitializeComponent();

        _healthRecordService = new HealthRecordService();
        _vaccines = vaccines;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitialized) return;

        try
        {
            _healthRecord = await _healthRecordService.GetRecordBySusAsync(_vaccines.SusNumber);
            susNumber = _healthRecord.SusNumber;

            viewModel = new VaccinesPageViewModel(susNumber);
            await viewModel.InitializeAsync();

            BindingContext = viewModel;
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.ShowPopupAsync(
                new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, "")
            );
            Console.WriteLine(ex);
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