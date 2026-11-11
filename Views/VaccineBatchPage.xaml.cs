using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class VaccineBatchPage : ContentPage
{
    private readonly VaccineBatchViewModel _viewModel;
    private bool _isClosing;

    public VaccineBatchPage(VaccineBatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void CancelButton_Clicked(object sender, EventArgs e) => await CloseAsync();

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        if (!_isClosing && await _viewModel.SaveAsync())
        {
            await CloseAsync();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        return _viewModel.IsBusy || _isClosing || base.OnBackButtonPressed();
    }

    private async Task CloseAsync()
    {
        if (_isClosing || _viewModel.IsBusy)
        {
            return;
        }

        _isClosing = true;
        try
        {
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            _isClosing = false;
            await DisplayAlertAsync("Erro", ex.Message, "Voltar");
        }
    }
}
