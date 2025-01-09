using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public partial class HousesPageViewModel : BaseViewModel
    {
        private readonly HouseService _houseService;
        private readonly HealthRecordService _healthRecordService;

        private CancellationTokenSource _throttleCts;

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        // Coleção diretamente vinculada à UI
        public ObservableCollection<House> Houses { get; } = new ObservableCollection<House>();

        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand FamilyCommand { get; }
        public ICommand LoadHousesCommand { get; }
        public ICommand NewHouseCommand { get; }

        public HousesPageViewModel() { }

        public HousesPageViewModel(DatabaseService databaseService, HouseService houseService)
        {
            _houseService = houseService ?? throw new ArgumentNullException(nameof(houseService));
            _healthRecordService = new HealthRecordService(databaseService);

            DeleteCommand = new Command<int>(async id => await DeleteHouseAsync(id));
            EditCommand = new Command<int>(async id => await EditHouseAsync(id, databaseService));
            FamilyCommand = new Command<int>(async id => await OpenFamilyPageAsync(id, databaseService));
            LoadHousesCommand = new Command<string>(async _ => await LoadHousesAsync(SearchText));
            NewHouseCommand = new Command(async _ => await CreateHouseAsync(databaseService));

            // Carregamento inicial de dados
            Task.Run(() => LoadHousesAsync(string.Empty));
        }

        private async Task LoadHousesAsync(string search)
        {
            try
            {
                // Cancelar qualquer busca anterior em andamento
                _throttleCts?.Cancel();
                _throttleCts = new CancellationTokenSource();

                // Adicionar um pequeno atraso (debounce) para evitar chamadas excessivas
                await Task.Delay(300, _throttleCts.Token); // Atraso de 300ms

                _throttleCts.Token.ThrowIfCancellationRequested();

                var houses = await _houseService.GetAllHousesAsync();

                IEnumerable<House> filteredHouses;

                // Verificar se o texto de pesquisa não está vazio
                if (string.IsNullOrWhiteSpace(search))
                {
                    filteredHouses = houses;
                }
                else
                {
                    // Dividir o texto de pesquisa em palavras
                    var searchParts = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // Verificar se o último item é um número
                    var possibleNumber = searchParts.LastOrDefault();
                    bool isNumber = int.TryParse(possibleNumber, out int number);

                    string streetSearch = string.Join(" ", searchParts.Take(searchParts.Length - (isNumber ? 1 : 0))).ToLowerInvariant();

                    // Filtrar por rua e, se houver, também pelo número
                    filteredHouses = houses.Where(h =>
                    {
                        bool matchesStreet = string.IsNullOrEmpty(streetSearch) || (h.Rua?.ToLowerInvariant().Contains(streetSearch) ?? false);
                        bool matchesNumber = !isNumber || (h.NumeroCasa != null && h.NumeroCasa == possibleNumber);

                        // Retorna as casas que atendem aos critérios de rua e número
                        return matchesStreet && matchesNumber;
                    }).ToList();
                }

                // Aplicar ordenação por Rua, Número e Complemento
                var orderedHouses = filteredHouses
                    .OrderBy(h => h.Rua?.ToLowerInvariant()) // Ordenar por rua (case-insensitive)
                    .ThenBy(h => int.TryParse(h.NumeroCasa, out var numero) ? numero : int.MaxValue) // Ordenar por número (tratando strings como inteiros)
                    .ThenBy(h => h.Complemento?.ToLowerInvariant()); // Ordenar por complemento (case-insensitive)

                // Atualiza a coleção de casas na UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Houses.Clear();
                    foreach (var house in orderedHouses)
                    {
                        Houses.Add(house);
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // A tarefa foi cancelada devido à pesquisa ser reiniciada
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
        }

        private async Task DeleteHouseAsync(int id)
        {
            try
            {
                bool confirm = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp(
                        "Confirmar",
                        "Tem certeza de que deseja excluir a residência?\n\nTodas as famílias desta casa serão perdidas.",
                        true,
                        "Excluir",
                        true,
                        "Cancelar"
                    )));

                if (confirm) return;

                // Obter registros associados à residência
                var people = await _healthRecordService.GetRecordsByHouseID(id);

                foreach (var person in people)
                {
                    person.HouseId = 0;
                    person.FamilyId = 0;
                    await _healthRecordService.UpdateRecordAsync(person);
                }

                await _houseService.DeleteHouseAsync(id);

                // Atualizar a lista após exclusão
                await LoadHousesAsync(string.Empty);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async Task EditHouseAsync(int id, DatabaseService databaseService)
        {
            try
            {
                var house = await _houseService.GetHousesById(id);
                if (house != null)
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new AddHouse(house, databaseService, id));
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async Task CreateHouseAsync(DatabaseService databaseService)
        {
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(new AddHouse(databaseService));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async Task OpenFamilyPageAsync(int id, DatabaseService databaseService)
        {
            try
            {
                var familyViewModel = new FamiliesViewModel(id);
                await Application.Current.MainPage.Navigation.PushAsync(new FamiliesPage(familyViewModel));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }
    }
}
