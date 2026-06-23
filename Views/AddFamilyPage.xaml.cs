using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class AddFamilyPage : ContentPage
{
    private readonly AddFamilyViewModel viewModel;

    public AddFamilyPage(
        int idHouse,
        bool isEdit,
        int? idFamily,
        IFamilyService familyService,
        IFamilyManager familyManager,
        IPatientService patientService)
    {
        InitializeComponent();
        BindingContext = viewModel = new AddFamilyViewModel(
            idHouse,
            isEdit,
            idFamily,
            familyService,
            familyManager,
            patientService);
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = viewModel.LoadDataAsync();
    }

    private async void Entry_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            await viewModel.SearchAsync(e.NewTextValue);
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "Voltar");
        }
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        Entry_Search.Text = string.Empty;
    }
}
