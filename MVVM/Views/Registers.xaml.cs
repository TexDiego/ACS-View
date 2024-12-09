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
    private string _filter;
    private string _order;

    public Registers(string condition, DatabaseService databaseService, HealthRecordService healthRecordService, AddRegisterViewModel addRegisterViewModel)
	{
		InitializeComponent();

        try
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _healthRecordService = healthRecordService ?? throw new ArgumentNullException(nameof(healthRecordService));
            _addRegisterViewModel = addRegisterViewModel ?? throw new ArgumentNullException(nameof(addRegisterViewModel));
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

        _order = "Crescente";
        _filter = "Nome";

        try
        {
            _addRegisterViewModel.IsLoading = true;

            // Recarregar dados sempre que a página aparecer
            viewModel = new RegistersViewModel(_healthRecordService, _condition, string.Empty, _filter, _order);
            BindingContext = viewModel;
            await Task.Run(() => viewModel.UpdateDatas(_condition, SB.Text, _filter, _order));

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
        _throttleCts?.Cancel();
        _throttleCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _throttleCts.Token);
            _throttleCts.Token.ThrowIfCancellationRequested();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                viewModel.UpdateDatas(_condition, e.NewTextValue, _filter, _order);
            });
        }
        catch (TaskCanceledException)
        {
            // Ignore exception, tarefa foi cancelada
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro de pesquisa", ex.Message, "Voltar");
        }
    }

    private async void Btn_filter_Clicked(object sender, EventArgs e)
    {
        var selectedOption = await DisplayActionSheet("Ordenar conteúdo por:", "Voltar", null, "Nome", "Idade");

        if (selectedOption == "Voltar" || string.IsNullOrEmpty(selectedOption))
            return; // Não faz nada se o usuário escolheu "Voltar"

        if (selectedOption == Btn_filter.Text)
            return;

        _filter = selectedOption;
        Btn_filter.Text = $"Ordenar por {_filter}";

        await RefreshCollectionAsync();
    }

    private async void Btn_order_Clicked(object sender, EventArgs e)
    {
        var selectedOption = await DisplayActionSheet("Ordenar por ordem:", "Voltar", null, "Crescente", "Decrescente");

        if (selectedOption == "Voltar" || string.IsNullOrEmpty(selectedOption))
            return; // Não faz nada se o usuário escolheu "Voltar"

        if (selectedOption == Btn_order.Text)
            return;

        _order = selectedOption;
        Btn_order.Text = $"Ordem {_order}";

        await RefreshCollectionAsync();
    }

    private async Task RefreshCollectionAsync()
    {
        try
        {
            _addRegisterViewModel.IsLoading = true;
            await Task.Run(() => viewModel.UpdateDatas(_condition, SB.Text, _filter, _order));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "Voltar");
        }
        finally
        {
            _addRegisterViewModel.IsLoading = false;
        }
    }
}