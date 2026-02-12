using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace ACS_View.MVVM.ViewModels
{
    public class AllVisitsViewModel : INotifyPropertyChanged
    {
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();
        private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();
        private readonly VisitsService _visitsService = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public List<Visits> VisitsList { get; set; } = [];
        public List<House> HouseList { get; set; } = [];


        public IRelayCommand DeleteVisit { get; }
        public IRelayCommand GoToHouseCommand { get; }


        public AllVisitsViewModel()
        {
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

                Console.WriteLine($"Visita: {visit}");

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
                var house = await _houseService.GetHouseByIdAsync(id);

                if (house != null)
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new FamiliesPage(house.CasaId));
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