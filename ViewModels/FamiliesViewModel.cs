using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class FamiliesViewModel : BaseViewModel
    {
        private readonly IHouseService _houseService = App.StaticServiceProvider.GetRequiredService<IHouseService>();
        private readonly IVisitsService _visitsService = App.StaticServiceProvider.GetRequiredService<IVisitsService>();
        private readonly IPatientService _patientService = App.StaticServiceProvider.GetRequiredService<IPatientService>();
        private DateTime _suppressReloadUntilUtc = DateTime.MinValue;
        private int _loadedVersion = -1;
        private bool _hasLoaded;

        [ObservableProperty] private ObservableCollection<Familia> families = [];
        [ObservableProperty] private string house = "";

        public IRelayCommand AddFamilyCommand => new RelayCommand(AddFamily);
        public IRelayCommand<int> DeleteFamilyCommand => new RelayCommand<int>(DeleteFamily);
        public IRelayCommand<int> EditFamilyCommand => new RelayCommand<int>(EditFamily);
        public IRelayCommand<int> VisitFamilyCommand => new RelayCommand<int>(VisitFamily);
        public ICommand PersonInfo => new Command<int>(async (id) => await PersonData(id));
        public ICommand GoBack => new Command(async () => await NavigateBackAsync());


        private readonly int _idHouse = 0;

        public FamiliesViewModel(int idHouse)
        {
            _idHouse = idHouse;
        }

        internal bool ShouldSkipTransientReload()
        {
            return DateTime.UtcNow <= _suppressReloadUntilUtc;
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

                    Console.WriteLine(House);

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
                await PushPageAsync(new AddFamilyPage(_idHouse, false));
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
                    person.HouseId = 0;
                    person.FamilyId = 0;

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

                await PushPageAsync(new AddFamilyPage(_idHouse, true, idFamily));
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

                var popupResult = await Shell.Current.ShowPopupAsync<Visits>(new VisitPage(_idHouse, idFamily), PopupConfigs.Default);

                if (popupResult is null || popupResult.WasDismissedByTappingOutsideOfPopup || popupResult.Result is null)
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
                PersonsInfoViewModel vm = App.StaticServiceProvider.GetRequiredService<PersonsInfoViewModel>();
                var record = await _patientService.GetPatientById(Id);

                if (record != null)
                {
                    var popup = new PersonsInfo(vm);
                    popup.SetPatient(record);
                    SuppressTransientReload();
                    try
                    {
                        await Shell.Current.ShowPopupAsync(popup);
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
            _suppressReloadUntilUtc = DateTime.UtcNow.AddSeconds(2);
        }
    }
}
