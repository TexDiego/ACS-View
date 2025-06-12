using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace ACS_View.MVVM.ViewModels
{
    public class AllVisitsViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly HouseService _houseService;
        private readonly FamilyService _familyService;
        private readonly HealthRecordService _healthRecordService;
        private VisitsService _visitsService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public List<Visits> VisitsList { get; set; } = new List<Visits>();
        public List<House> HouseList { get; set; } = new List<House>();


        public IRelayCommand DeleteVisit { get; }
        public IRelayCommand GoToHouseCommand { get; }


        public AllVisitsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "O serviço de banco de dados não pode ser nulo.");
            _visitsService = new VisitsService(_databaseService);
            _houseService = new HouseService(_databaseService);
            _familyService = new FamilyService(_databaseService);
            _healthRecordService = new HealthRecordService(_databaseService);

            DeleteVisit = new RelayCommand<int>(async id => await DeleteVisitCommand(id));
            GoToHouseCommand = new RelayCommand<int>(async id => await GoToHouse(id));

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadVisitsAsync();
            });
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

                Console.WriteLine($"Visita: {visit.ToString()}");

                if (visit != null)
                {
                    await _visitsService.DeleteVisitAsync(id);
                    VisitsList.Remove(visit);
                    await LoadVisitsAsync();
                    return;
                }

                Console.WriteLine($"Nenhuma visita encontrada com o ID {id}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir visita: {ex.Message}");
            }
        }

        internal async Task LoadVisitsAsync()
        {
            try
            {
                VisitsList = await _visitsService.GetAllVisitsAsync();
                HouseList = await GetHousesWithoutVisits();
                OnPropertyChanged(nameof(VisitsList));
                OnPropertyChanged(nameof(HouseList));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar visitas: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível carregar as visitas.", "OK");
            }
        }

        private async Task GoToHouse(int id)
        {
            if (id < 0) return;

            try
            {
                var _familiesViewModel = new FamiliesViewModel(id);

                var house = await _houseService.GetHousesById(id);

                if (house != null)
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new FamiliesPage(_familiesViewModel));
                    OnPropertyChanged(nameof(HouseList));
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "Casa não encontrada.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao navegar para a casa: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível navegar para a casa.", "OK");
            }
        }

        public async Task<List<House>> GetHousesWithoutVisits()
        {
            try
            {
                var allHouses = await _houseService.GetAllHousesAsync();
                var allPeople = await _healthRecordService.GetAllRecordsAsync();

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

                // ✅ Se não houver casas, adicionar uma "dummy house"
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
                Console.WriteLine($"Erro ao buscar casas com famílias sem visita: {ex.Message}");
                return new List<House>();
            }
        }

        private void OnPropertyChanged(string v)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
        }
    }
}
