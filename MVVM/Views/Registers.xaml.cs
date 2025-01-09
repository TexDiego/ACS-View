using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class Registers : ContentPage
{
    private CancellationTokenSource _throttleCts;
    private readonly DatabaseService _databaseService;
    private RegistersViewModel viewModel;
    private readonly HealthRecordService _healthRecordService;
    private readonly AddRegisterViewModel _addRegisterViewModel;
    private string _condition;
    private string _filter;
    private string _order;

    public Registers(
        string condition,
        DatabaseService databaseService,
        HealthRecordService healthRecordService,
        AddRegisterViewModel addRegisterViewModel)
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
            this.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
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
            viewModel = new RegistersViewModel(_healthRecordService, _databaseService, _condition, string.Empty, _filter, _order);
            BindingContext = viewModel;
            await Task.Run(() => viewModel.UpdateDatasAsync(_condition, SB.Text, _filter, _order));

            Lbl_Title.Text = CapitalizeTitle(_condition);
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
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
        await Navigation.PushAsync(new AddRegister(_databaseService));
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
                viewModel.UpdateDatasAsync(_condition, e.NewTextValue, _filter, _order);
            });
        }
        catch (TaskCanceledException)
        {
            // Ignore exception, tarefa foi cancelada
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private async void Btn_filter_Clicked(object sender, EventArgs e)
    {
        //var selectedOption = await DisplayActionSheet("Ordenar conteúdo por:", "Voltar", null, "Nome", "Idade");

        var selectedOption = await this.ShowPopupAsync(new DisplaySheetPopUp("Ordenar por:", "Nome", "Idade", "Voltar", 1));

        if (selectedOption == null || Convert.ToString(selectedOption) == Btn_filter.Text.Substring(12))
            return;

        _filter = Convert.ToString(selectedOption) ?? string.Empty;

        Btn_filter.Text = $"Ordenar por {_filter}";

        await RefreshCollectionAsync();
    }

    private async void Btn_order_Clicked(object sender, EventArgs e)
    {
        var selectedOption = await this.ShowPopupAsync(new DisplaySheetPopUp("Ordenar em:", "Crescente", "Decrescente", "Voltar", 2));

        if (selectedOption == null || Convert.ToString(selectedOption) == Btn_order.Text.Substring(6))
            return;

        _order = Convert.ToString(selectedOption) ?? string.Empty;

        Btn_order.Text = $"Ordem {_order}";

        await RefreshCollectionAsync();
    }

    private async Task RefreshCollectionAsync()
    {
        try
        {
            _addRegisterViewModel.IsLoading = true;
            await Task.Run(() => viewModel.UpdateDatasAsync(_condition, SB.Text, _filter, _order));
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
        finally
        {
            _addRegisterViewModel.IsLoading = false;
        }
    }
}