using ACS_View.Domain.Interfaces;
using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class Registers : ContentPage, IQueryAttributable
{
    private readonly AddRegisterViewModel _addRegisterViewModel;
    private readonly RegistersViewModel _viewModel;

    private string _condition = "ALL";
    private bool _hasAppeared;
    private int _loadVersion;

    public Registers(
        IPatientService patientService,
        ICidRepository cidRepo,
        IPatientCidRepository patientCid,
        ISQLiteConditionsRepository conditionsRepository,
        RegistersViewModel viewModel)
    {
        InitializeComponent();
        _addRegisterViewModel = new(patientService, cidRepo, patientCid, conditionsRepository);
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("condition", out var condition))
        {
            _condition = condition?.ToString() ?? "ALL";
            _viewModel.SetFilter(_condition);

            if (_hasAppeared)
            {
                _ = LoadDataOnAppearAsync();
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _hasAppeared = true;

        if (_viewModel.ShouldSkipTransientReload())
        {
            return;
        }

        _ = LoadDataOnAppearAsync();
    }

    private async Task LoadDataOnAppearAsync()
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);

        try
        {
            _addRegisterViewModel.IsLoading = true;

            await _viewModel.LoadPatients();
            if (loadVersion != _loadVersion)
            {
                return;
            }

            await ScrollToTargetIfNeeded();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "Voltar");
        }
        finally
        {
            if (loadVersion == _loadVersion)
            {
                _addRegisterViewModel.IsLoading = false;
            }
        }
    }

    private async Task ScrollToTargetIfNeeded()
    {
        if (_viewModel.ScrollToId > 0)
        {
            var item = _viewModel.Patients.FirstOrDefault(r => r.Id == _viewModel.ScrollToId);
            if (item != null)
            {
                await Task.Delay(100);
                collectionView.ScrollTo(item, position: ScrollToPosition.Center, animate: true);
                _viewModel.ScrollToId = 0;
            }
        }
    }

    private void SB_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchPatientsCommand.Execute(e.NewTextValue);
    }
}
