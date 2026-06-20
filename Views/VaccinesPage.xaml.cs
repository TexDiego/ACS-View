using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class VaccinesPage : ContentPage, IQueryAttributable
{
    private readonly VaccinesPageViewModel _viewModel;
    private int? _patientId;
    private bool _isVisible;
    private int _navigationVersion;

    public VaccinesPage(VaccinesPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("patientId", out var patientId))
        {
            _patientId = Convert.ToInt32(patientId);
            _navigationVersion++;
        }

        if (_isVisible)
        {
            QueuePatientLoad();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isVisible = true;
        QueuePatientLoad();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isVisible = false;
    }

    private void QueuePatientLoad()
    {
        if (_patientId is not int patientId)
        {
            return;
        }

        var navigationVersion = _navigationVersion;

        Dispatcher.Dispatch(async () =>
        {
            await Task.Yield();

            if (!_isVisible || navigationVersion != _navigationVersion)
            {
                return;
            }

            try
            {
                await _viewModel.LoadPatientAsync(patientId);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "Voltar");
                Console.WriteLine(ex);
            }
        });
    }

    private async void Btn_GoBack_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
