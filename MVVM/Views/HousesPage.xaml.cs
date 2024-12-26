using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class HousesPage : ContentPage
{
    private CancellationTokenSource _throttleCts;
    private HousesPageViewModel viewModel;
    private readonly HouseService _houseService;
    private readonly AddHouseViewModel _addHouseViewModel;
    private readonly DatabaseService _databaseService;
    string _order;

    public HousesPage(
        DatabaseService databaseService,
        HouseService houseService,
        AddHouseViewModel addHouseViewModel)
    {
        InitializeComponent();

        try
        {
            _order = "Crescente";
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _houseService = houseService ?? throw new ArgumentNullException(nameof(houseService));
            _addHouseViewModel = addHouseViewModel ?? throw new ArgumentNullException(nameof(addHouseViewModel));
        }
        catch (Exception ex)
        {
            this.ShowPopup(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _order = "Crescente";

        try
        {
            _addHouseViewModel.IsLoading = true;

            // Recarregar dados sempre que a página aparecer
            viewModel = new HousesPageViewModel(_databaseService,_houseService, "", _order);
            BindingContext = viewModel;

            await Task.Run(() => viewModel.UpdateDatas(SB.Text, _order));
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
        finally
        {
            _addHouseViewModel.IsLoading = false;
        }
    }

    private void Btn_Voltar_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private async void Btn_AddHouse_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddHouse(_databaseService));
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
                viewModel.UpdateDatas(e.NewTextValue, _order);
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
            _addHouseViewModel.IsLoading = true;
            await Task.Run(() => viewModel.UpdateDatas(SB.Text, _order));
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
        finally
        {
            _addHouseViewModel.IsLoading = false;
        }
    }
}