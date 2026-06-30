using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;
using System.Diagnostics;

namespace ACS_View.Views;

public partial class AddRegister : ContentPage, IQueryAttributable
{
    private readonly AddRegisterViewModel _viewModel;
    private int? _patientId;
    private bool _loaded;
    private bool _isLoading;

    public AddRegister(
        IPatientService patientService,
        ICidRepository cidRepo,
        IPatientCidRepository patientCid,
        ISQLiteConditionsRepository conditionsRepository,
        IPopupService popupService)
    {
        InitializeComponent();
        BindingContext = _viewModel = new AddRegisterViewModel(patientService, cidRepo, patientCid, conditionsRepository, popupService);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("patientId", out var patientId))
        {
            SetPatientId(Convert.ToInt32(patientId));
        }

        if (query.TryGetValue("record", out var record) && record is Patient p)
        {
            SetPatientId(p.Id);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadPageDataAsync();
    }

    private async Task LoadPageDataAsync()
    {
        if (_loaded || _isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            if (!(_viewModel.Subcategories is { Count: > 0 }))
            {
                await _viewModel.LoadSubcategories();
            }

            if (_patientId is int id)
            {
                await _viewModel.LoadPatient(id);
            }

            _loaded = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _viewModel.ShowLoadErrorAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void SetPatientId(int patientId)
    {
        if (_patientId == patientId)
        {
            return;
        }

        _patientId = patientId;
        _loaded = false;
    }
}
