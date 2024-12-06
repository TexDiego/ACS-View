using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModel;

namespace ACS_View.MVVM.Views;

public partial class Registers : ContentPage
{
    private CancellationTokenSource _throttleCts;
    private RegistersViewModel viewModel;
    private DatabaseService _databaseService;
    private HealthRecordService _healthRecordService;
    private AddRegisterViewModel _addRegisterViewModel;
    private string _condition;

    public Registers(string condition)
	{
		InitializeComponent();

        try
        {
            _databaseService = new DatabaseService();
            _healthRecordService = new HealthRecordService(_databaseService);
            _addRegisterViewModel = new AddRegisterViewModel();
            _condition = condition;
        }
        catch (Exception ex)
        {
            DisplayAlert("Erro", ex.Message, "Voltar");
        }
    }

    private static string CapitalizeTitle(string cond)
    {
        return cond switch
        {
            "GESTANTE" => "Gestante",
            "HAS" => "Hipertensos",
            "DB" => "Diabéticos",
            "HASDB" => "Hipertensos Diabéticos",
            "HAN" => "Hanseníases",
            "TB" => "Tuberculosos",
            "ACAMADO" => "Domiciliados",
            "DOMICILIADO" => "Acamados",
            "MENOR" => "Menores de 2 anos",
            "MENTAL" => "Condições Mentais",
            "FUMANTE" => "Fumantes",
            "DEFICIENTE" => "Desabilitados",
            "IDOSO" => "Idosos",
            "CANCER" => "Câncer",
            _ => "Cadastros"
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        SB.Text = "";

        try
        {
            _addRegisterViewModel.IsLoading = true;

            viewModel = new RegistersViewModel(_healthRecordService, _condition, string.Empty);
            BindingContext = viewModel;
            Lbl_Title.Text = CapitalizeTitle(_condition);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro de OnAppearing", ex.Message, "Voltar");
        }
        finally
        {
            _addRegisterViewModel.IsLoading = false;
        }
    }

    private async void Btn_RegistersGoBack_Clicked(object sender, EventArgs e)
    {
		await Navigation.PopAsync();
    }

    private async void Btn_AddRegister_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddRegister());
    }

    private async void SB_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            _throttleCts?.Cancel();
            _throttleCts = new CancellationTokenSource();

            await Task.Delay(300, _throttleCts.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                            viewModel.UpdateDatas(_condition, e.NewTextValue));
                    }
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro de SearchBar", ex.Message, "Voltar");
        }
    }
}