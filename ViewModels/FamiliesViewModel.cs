using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class FamiliesViewModel : BaseViewModel
    {
        private readonly IHouseService _houseService;
        private readonly IVisitsService _visitsService;
        private readonly IPatientService _patientService;
        private readonly IPersonsInfoPopupService _personsInfoPopupService;
        private readonly IFamilyService _familyService;
        private readonly IFamilyManager _familyManager;
        private readonly IPopupService _popupService;
        private int _loadedVersion = -1;
        private bool _hasLoaded;
        private bool _skipNextAppearReload;
        private DateTime _suppressReloadUntilUtc = DateTime.MinValue;

        [ObservableProperty] private ObservableCollection<Familia> families = [];
        [ObservableProperty] private string house = "";

        public IRelayCommand AddFamilyCommand => new RelayCommand(AddFamily);
        public IRelayCommand<int> DeleteFamilyCommand => new RelayCommand<int>(DeleteFamily);
        public IRelayCommand<int> EditFamilyCommand => new RelayCommand<int>(EditFamily);
        public IRelayCommand<int> VisitFamilyCommand => new RelayCommand<int>(VisitFamily);
        public ICommand PersonInfo => new Command<int>(async (id) => await PersonData(id));
        public ICommand GoBack => new Command(async () => await NavigateBackAsync());


        private readonly int _idHouse = 0;
        internal int HouseId => _idHouse;

        public FamiliesViewModel(
            int idHouse,
            IHouseService houseService,
            IVisitsService visitsService,
            IPatientService patientService,
            IPersonsInfoPopupService personsInfoPopupService,
            IFamilyService familyService,
            IFamilyManager familyManager,
            IPopupService popupService)
        {
            _idHouse = idHouse;
            _houseService = houseService;
            _visitsService = visitsService;
            _patientService = patientService;
            _personsInfoPopupService = personsInfoPopupService;
            _familyService = familyService;
            _familyManager = familyManager;
            _popupService = popupService;
        }

        internal bool ShouldSkipTransientReload()
        {
            if (_skipNextAppearReload || DateTime.UtcNow <= _suppressReloadUntilUtc)
            {
                _skipNextAppearReload = false;
                return true;
            }

            return false;
        }

        public async Task LoadFamiliesAsync()
        {
            var currentVersion = DataChangeTracker.PatientsVersion + DataChangeTracker.HousesVersion;
            if (_hasLoaded && _loadedVersion == currentVersion)
            {
                return;
            }

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var house = await _houseService.GetHouseByIdAsync(_idHouse);

                    if (house == null)
                    {
                        await DisplayAlertAsync("Erro", "Casa não encontrada.");
                        return;
                    }

                    string rua = house.Rua ?? "Famílias";
                    string numero = house.NumeroCasa ?? "";
                    string complemento = house.Complemento ?? "";

                    House = complemento == "" ? $"{rua}\n Nº {numero}" : $"{rua}\n Nº {numero} - {complemento}";

                    var familiesFromDb = await _patientService.GetPatientsByHouseId(_idHouse);
                    var familiesGrouped = familiesFromDb
                        .Where(p => p.FamilyId > 0)
                        .GroupBy(p => p.FamilyId)
                        .Select(g => new Familia
                        {
                            IdFamily = g.Key,
                            PessoasFamilia = new ObservableCollection<Patient>([.. g])
                        }).ToList();

                    Families.Clear();

                    foreach (var family in familiesGrouped)
                    {
                        Families.Add(family);
                    }

                    _loadedVersion = currentVersion;
                    _hasLoaded = true;
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message);
            }
        }

        private async void AddFamily()
        {
            try
            {
                var newFamily = new Familia
                {
                    PessoasFamilia = [] // Cria uma nova lista de pessoas
                };

                Families.Add(newFamily); // Adiciona a nova família à coleção
                await PushPageAsync(new AddFamilyPage(
                    _idHouse,
                    false,
                    null,
                    _familyService,
                    _familyManager,
                    _patientService));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Não foi possível adicionar a nova família.\n\n{ex.Message}");
            }
        }

        private async void DeleteFamily(int idFamily)
        {
            try
            {
                var confirm = await DisplayConfirmationAsync(
                    "Confirmar Exclusão",
                    $"Tem certeza de que deseja excluir a família?",
                    "Excluir");

                if (!confirm) return;

                var pessoasDaFamilia = await _patientService.GetPatientsByFamilyAndHouseId(idFamily, _idHouse);

                foreach (var person in pessoasDaFamilia)
                {
                    person.HouseId = -1;
                    person.FamilyId = -1;
                    person.FamilyResponsibleSus = null;

                    await _patientService.UpdatePatient(person);
                }

                OnPropertyChanged(nameof(Families));
                await LoadFamiliesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Erro",
                    $"Não foi possível excluir a família.\n\n{ex.Message}",
                    "OK");
            }
        }

        public async void EditFamily(int idFamily)
        {
            try
            {
                var familyToEdit = Families.FirstOrDefault(f => f.IdFamily == idFamily);

                if (familyToEdit == null)
                {
                    await DisplayAlertAsync("Aviso", "Família não encontrada.");
                    return;
                }

                await PushPageAsync(new AddFamilyPage(
                    _idHouse,
                    true,
                    idFamily,
                    _familyService,
                    _familyManager,
                    _patientService));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Não foi possível editar a família.\n\n{ex.Message}");
            }
        }

        public async void VisitFamily(int idFamily)
        {
            try
            {
                var familyToVisit = Families.FirstOrDefault(f => f.IdFamily == idFamily);

                if (familyToVisit == null)
                {
                    await DisplayAlertAsync("Aviso", "Família não encontrada.");
                    return;
                }

                var popupResult = await _popupService.ShowAsync<Visits>(new VisitPage(_houseService, _idHouse, idFamily));

                if (popupResult.WasDismissed || popupResult.Result is null)
                {
                    return;
                }

                await _visitsService.RegisterVisitAsync(popupResult.Result);
                await DisplayAlertAsync("Sucesso", $"Visita registrada como {popupResult.Result.Description}.");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Não foi possível visitar a família.\n\n{ex.Message}");
            }
        }

        private async Task PersonData(int Id)
        {
            try
            {
                var record = await _patientService.GetPatientById(Id);

                if (record != null)
                {
                    SuppressTransientReload();
                    try
                    {
                        await _personsInfoPopupService.ShowAsync(record);
                    }
                    finally
                    {
                        SuppressTransientReload();
                    }
                }
            }
            catch
            {
                await DisplayAlertAsync("Erro", "Erro ao carregar os dados da pessoa.", "Fechar");
            }
        }

        private void SuppressTransientReload()
        {
            _skipNextAppearReload = true;
            _suppressReloadUntilUtc = DateTime.UtcNow.AddSeconds(2);
        }
    }
}
