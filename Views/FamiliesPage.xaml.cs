using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class FamiliesPage : ContentPage, IQueryAttributable
{
    private readonly IHouseService _houseService;
    private readonly IVisitsService _visitsService;
    private readonly IPatientService _patientService;
    private readonly IPersonsInfoPopupService _personsInfoPopupService;
    private readonly IFamilyService _familyService;
    private readonly IFamilyManager _familyManager;
    private readonly IPopupService _popupService;
    private FamiliesViewModel? _viewModel;
    private bool _hasAppeared;

    public FamiliesPage(
        IHouseService houseService,
        IVisitsService visitsService,
        IPatientService patientService,
        IPersonsInfoPopupService personsInfoPopupService,
        IFamilyService familyService,
        IFamilyManager familyManager,
        IPopupService popupService)
    {
        InitializeComponent();
        _houseService = houseService;
        _visitsService = visitsService;
        _patientService = patientService;
        _personsInfoPopupService = personsInfoPopupService;
        _familyService = familyService;
        _familyManager = familyManager;
        _popupService = popupService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetHouseId(query, out var houseId))
        {
            BindingContext = _viewModel = new FamiliesViewModel(
                houseId,
                _houseService,
                _visitsService,
                _patientService,
                _personsInfoPopupService,
                _familyService,
                _familyManager,
                _popupService);

            if (_hasAppeared)
            {
                LoadFamiliesIfReady();
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _hasAppeared = true;
        LoadFamiliesIfReady();
    }

    private void LoadFamiliesIfReady()
    {
        if (_viewModel != null)
        {
            if (_viewModel.ShouldSkipTransientReload())
            {
                return;
            }

            _ = _viewModel.LoadFamiliesAsync();
        }
    }

    private static bool TryGetHouseId(IDictionary<string, object> query, out int houseId)
    {
        houseId = 0;
        if (!query.TryGetValue("id", out var id) || id is null)
        {
            return false;
        }

        if (id is int intId)
        {
            houseId = intId;
            return houseId > 0;
        }

        return int.TryParse(id.ToString(), out houseId) && houseId > 0;
    }
}
