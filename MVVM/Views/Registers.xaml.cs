using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class Registers : ContentPage, IQueryAttributable
{
    private CancellationTokenSource _throttleCts = new();

    private readonly AddRegisterViewModel _addRegisterViewModel;
    private readonly RegistersViewModel viewModel = new();

    private string _condition = "Cadastros";
    private string _filter = "Nome";
    private string _order = "Crescente";

    public Registers(IDatabaseService _db)
    {
        InitializeComponent();
        _addRegisterViewModel = new(_db);
        BindingContext = viewModel = new RegistersViewModel();
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

    private async Task ShowErrorPopupAsync(string message)
    {
        await this.ShowPopupAsync(new DisplayPopUp("Erro", message, true, "Voltar", false, ""));
    }
}