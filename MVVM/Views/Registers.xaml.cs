using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class Registers : ContentPage
{
    private readonly INavigationService navigationService = App.ServiceProvider.GetRequiredService<INavigationService>();

    private CancellationTokenSource _throttleCts = new();

    private readonly AddRegisterViewModel _addRegisterViewModel = new();
    private readonly RegistersViewModel viewModel = new();

    private string _condition = "";
    private string _filter = "Nome";
    private string _order = "Crescente";

    public Registers()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _condition = navigationService.GetCondition();
        await LoadDataOnAppearAsync();
    }

    private async Task LoadDataOnAppearAsync()
    {
        try
        {
            _addRegisterViewModel.IsLoading = true;

            await viewModel.InitAsync(_condition, SB.Text, _filter, _order);
            await ScrollToTargetIfNeeded();
            Lbl_Title.Text = CapitalizeTitle(_condition);
        }
        catch (Exception ex)
        {
            await ShowErrorPopupAsync(ex.Message);
        }
        finally
        {
            _addRegisterViewModel.IsLoading = false;
        }
    }

    private async Task ScrollToTargetIfNeeded()
    {
        if (!string.IsNullOrEmpty(viewModel.ScrollToSusNumber))
        {
            var item = viewModel.HealthRecords.FirstOrDefault(r => r.SusNumber == viewModel.ScrollToSusNumber);
            if (item != null)
            {
                await Task.Delay(100);
                collectionView.ScrollTo(item, position: ScrollToPosition.Center, animate: true);
                viewModel.ScrollToSusNumber = null;
            }
        }
    }

    private async void SB_TextChanged(object sender, TextChangedEventArgs e)
    {
        _throttleCts?.Cancel();
        _throttleCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _throttleCts.Token);
            _throttleCts.Token.ThrowIfCancellationRequested();

            await viewModel.InitAsync(_condition, e.NewTextValue, _filter, _order);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            await ShowErrorPopupAsync(ex.Message);
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
            "ACAMADO" => "Acamados",
            "DOMICILIADO" => "Domiciliados",
            "MENOR" => "Menores de 6 anos",
            "MENTAL" => "Condições Mentais",
            "BOLSA" => "Beneficiários de Bolsa Família",
            "CORACAO" => "Cardíacos",
            "FIGADO" => "Hepatopatas",
            "RIM" => "Renais",
            "PULMAO" => "Pulmonares",
            "NEURO" => "Neurodivergentes",
            "HIV" => "Imunodeficientes",
            "DROGAS" => "Dependentes químicos",
            "FUMANTE" => "Fumantes",
            "ALCOOLATRA" => "Álcoolatras",
            "DEFICIENTE" => "Desabilitados",
            "IDOSO" => "Idosos",
            "CANCER" => "Câncer",
            "NOHOME" => "Sem residência",
            _ => "Cadastros"
        };
    }

    private async Task<string?> ShowOptionsPopup(string title, string[] options, string currentValue)
    {
        var selected = await this.ShowPopupAsync(new DisplaySheetPopUp(title, options[0], options[1], "Voltar", 1));
        var value = Convert.ToString(selected);
        return value == currentValue ? null : value;
    }

    private async Task ShowErrorPopupAsync(string message)
    {
        await this.ShowPopupAsync(new DisplayPopUp("Erro", message, true, "Voltar", false, ""));
    }
}