using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class VaccinesPage : ContentPage
{
    VaccinesPageViewModel viewModel = new();
    private Patient _healthRecord;
    private readonly Vaccines _vaccines;
    private readonly IPatientService _patientService = App.ServiceProvider.GetRequiredService<IPatientService>();

    private int id;
    private bool _isInitialized = false;


    public VaccinesPage(Vaccines vaccines)
	{
        InitializeComponent();
        _vaccines = vaccines;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitialized) return;

        try
        {
            _healthRecord = await _patientService.GetPatientById(_vaccines.Id);
            id = _healthRecord.Id;

            viewModel = new VaccinesPageViewModel(id);
            await viewModel.InitializeAsync();

            BindingContext = viewModel;
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.ShowPopupAsync(
                new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, "")
            );
            Console.WriteLine(ex);
        }
    }


    private async void Btn_GoBack_Clicked(object sender, EventArgs e)
    {
		try
		{
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Shell.Current.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}