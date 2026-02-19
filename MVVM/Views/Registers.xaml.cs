using ACS_View.MVVM.Models.HealthConditions;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;
using System.Threading.Tasks;

namespace ACS_View.MVVM.Views;

public partial class Registers : ContentPage, IQueryAttributable
{
    private readonly IDatabaseService _databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();

    private CancellationTokenSource _throttleCts = new();

    private readonly AddRegisterViewModel _addRegisterViewModel = new();
    private readonly RegistersViewModel viewModel = new();

    private string _condition = "Cadastros";
    private string _filter = "Nome";
    private string _order = "Crescente";

    public Registers()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("condition", out var condition))
            _condition = (string)condition;        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataOnAppearAsync();
    }

    private async Task LoadDataOnAppearAsync()
    {
        try
        {
            _addRegisterViewModel.IsLoading = true;

            await viewModel.InitAsync(_condition, SB.Text, _filter, _order);
            await ScrollToTargetIfNeeded();
            viewModel.Condition = _condition;
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
        if (viewModel.ScrollToId < 1)
        {
            var item = viewModel.Patients.FirstOrDefault(r => r.Id == viewModel.ScrollToId);
            if (item != null)
            {
                await Task.Delay(100);
                collectionView.ScrollTo(item, position: ScrollToPosition.Center, animate: true);
                viewModel.ScrollToId = 0;
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

    private async Task<string> CapitalizeTitle(int id)
    {
        var condition = await _databaseService.Connection.Table<ConditionCategory>().FirstOrDefaultAsync(c => c.Id == id);

        return condition.Name;

        //return cond switch
        //{
        //    "GESTANTE" => "Gestante",
        //    "HAS" => "Hipertensos",
        //    "DB" => "Diabéticos",
        //    "HASDB" => "Hipertensos Diabéticos",
        //    "HAN" => "Hanseníases",
        //    "TB" => "Tuberculosos",
        //    "ACAMADO" => "Acamados",
        //    "DOMICILIADO" => "Domiciliados",
        //    "MENOR" => "Menores de 6 anos",
        //    "MENTAL" => "Condições Mentais",
        //    "BOLSA" => "Beneficiários de Bolsa Família",
        //    "CORACAO" => "Cardíacos",
        //    "FIGADO" => "Hepatopatas",
        //    "RIM" => "Renais",
        //    "PULMAO" => "Pulmonares",
        //    "NEURO" => "Neurodivergentes",
        //    "HIV" => "Imunodeficientes",
        //    "DROGAS" => "Dependentes químicos",
        //    "FUMANTE" => "Fumantes",
        //    "ALCOOLATRA" => "Álcoolatras",
        //    "DEFICIENTE" => "Desabilitados",
        //    "IDOSO" => "Idosos",
        //    "CANCER" => "Câncer",
        //    "NOHOME" => "Sem residência",
        //    _ => "Cadastros"
        //};
    }

    private async Task ShowErrorPopupAsync(string message)
    {
        await this.ShowPopupAsync(new DisplayPopUp("Erro", message, true, "Voltar", false, ""));
    }
}