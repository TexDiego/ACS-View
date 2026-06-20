using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class AllVisitsViewModel : BaseViewModel
    {
        private readonly IHouseService _houseService;
        private readonly IVisitsService _visitsService;
        private readonly IPatientService _patientService;
        private int _loadedVersion = -1;
        private bool _hasLoaded;

        [ObservableProperty] private List<Visits> visitsList = [];
        [ObservableProperty] private List<House> houseList = [];

        public ICommand DeleteVisit => new Command<int>(async id => await DeleteVisitCommand(id));
        public ICommand GoToHouseCommand => new Command<int>(async id => await GoToHouse(id));

        public AllVisitsViewModel(IHouseService houseService, IVisitsService visitsService, IPatientService patientService)
        {
            _houseService = houseService;
            _visitsService = visitsService;
            _patientService = patientService;
        }

        private async Task DeleteVisitCommand(int id)
        {
            try
            {
                if (id <= 0)
                {
                    throw new ArgumentException("O ID da visita deve ser maior que zero.");
                }

                var visit = VisitsList.FirstOrDefault(v => v.Id == id);

                Debug.WriteLine($"Visita: {visit}");

                if (visit != null)
                {
                    await _visitsService.DeleteVisitAsync(id);
                    VisitsList.Remove(visit);
                    await LoadVisitsAsync();
                    return;
                }

                Debug.WriteLine($"Nenhuma visita encontrada com o ID {id}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao excluir visita: {ex.Message}");
            }
        }

        internal async Task LoadVisitsAsync()
        {
            var currentVersion = DataChangeTracker.VisitsVersion + DataChangeTracker.PatientsVersion + DataChangeTracker.HousesVersion;
            if (_hasLoaded && _loadedVersion == currentVersion)
            {
                return;
            }

            try
            {
                VisitsList = await _visitsService.GetAllVisitsAsync();
                HouseList = await GetHousesWithoutVisits();
                _loadedVersion = currentVersion;
                _hasLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar visitas: {ex.Message}");
                await DisplayAlertAsync("Erro", "Não foi possível carregar as visitas.");
            }
        }

        private async Task GoToHouse(int id)
        {
            if (id < 0) return;

            try
            {
                await NavigateToAsync("families", new Dictionary<string, object> { { "id", id } });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao navegar para a casa: {ex.Message}");
                await DisplayAlertAsync("Erro", "Não foi possível navegar para a casa.");
            }
        }

        public async Task<List<House>> GetHousesWithoutVisits()
        {
            try
            {
                var allHouses = await _houseService.GetAllHousesAsync();
                var allPeople = await _patientService.GetAllPatients();

                var allFamilies = allPeople
                    .Where(p => p.HouseId > 0 && p.FamilyId > 0)
                    .Select(p => new { p.HouseId, p.FamilyId })
                    .Distinct()
                    .ToList();

                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                var visitedFamilies = VisitsList
                    .Where(v => v.Date.Month == currentMonth && v.Date.Year == currentYear)
                    .Select(v => new { v.HouseId, v.FamilyId })
                    .Distinct()
                    .ToHashSet();

                var familiesWithoutVisit = allFamilies
                    .Where(f => !visitedFamilies.Contains(f))
                    .ToList();

                var houseIdsWithoutVisit = familiesWithoutVisit
                    .Select(f => f.HouseId)
                    .Distinct()
                    .ToHashSet();

                var result = allHouses
                    .Where(h => houseIdsWithoutVisit.Contains(h.CasaId))
                    .ToList();

                // Se não houver casas, adicionar uma "dummy house"
                if (result.Count == 0)
                {
                    result.Add(new House
                    {
                        CasaId = -1,
                        Rua = "Todas as famílias foram visitadas neste mês.",
                        NumeroCasa = "",
                        Bairro = "",
                        Cidade = "",
                        Estado = "",
                        CEP = "",
                        Complemento = "",
                        PossuiComplemento = false,
                        Pais = "Brasil"
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar casas com famílias sem visita: {ex.Message}");
                return [];
            }
        }
    }
}

