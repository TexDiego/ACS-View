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
        IPatientBolsaFamiliaRepository bolsaFamiliaRepository,
        IPatientInsulinDependencyRepository insulinDependencyRepository,
        IPregnancyService pregnancyService,
        ICareNotificationService careNotificationService,
        IPopupService popupService)
    {
        InitializeComponent();
        BindingContext = _viewModel = new AddRegisterViewModel(patientService, cidRepo, patientCid, conditionsRepository, bolsaFamiliaRepository, insulinDependencyRepository, pregnancyService, careNotificationService, popupService);
    }

    private bool? _sexOptionsHorizontal;

    private void OnSexOptionsSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Grid grid || grid.Width <= 0)
        {
            return;
        }

        // Allow room for all three labels; use full-width options on small screens.
        var horizontal = grid.Width >= 480;
        if (_sexOptionsHorizontal == horizontal)
        {
            return;
        }

        _sexOptionsHorizontal = horizontal;
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        for (var index = 0; index < (horizontal ? 1 : 3); index++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var index = 0; index < (horizontal ? 3 : 1); index++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        for (var index = 0; index < grid.Children.Count; index++)
        {
            grid.SetRow(grid.Children[index], horizontal ? 0 : index);
            grid.SetColumn(grid.Children[index], horizontal ? index : 0);
        }
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
        InputFocusGuard.ClearTextInputFocus(this);
        _ = LoadPageDataAsync();
    }

    protected override void OnDisappearing()
    {
        InputFocusGuard.ClearTextInputFocus(this);
        base.OnDisappearing();
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
