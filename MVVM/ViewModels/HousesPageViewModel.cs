using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class HousesPageViewModel : BaseViewModel
    {
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();
        private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();

        private CancellationTokenSource _throttleCts = new();

        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private ObservableCollection<House> houses = [];

        public ICommand DeleteCommand => new Command<int>(async id => await DeleteHouseAsync(id));
        public ICommand EditCommand => new Command<int>(async id => await EditHouseAsync(id));
        public ICommand FamilyCommand => new Command<int>(async id => await OpenFamilyPageAsync(id));
        public ICommand LoadHousesCommand => new Command<string>(async _ => await LoadHousesAsync(SearchText));
        public ICommand NewHouseCommand => new Command(async _ => await CreateHouseAsync());

        public HousesPageViewModel()
        {
            MainThread.BeginInvokeOnMainThread(async () => await LoadHousesAsync(string.Empty));
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
                    var searchParts = search.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                    // Verificar se o último item é um número
                    var possibleNumber = searchParts.LastOrDefault();
                    bool isNumber = int.TryParse(possibleNumber, out int number);

                    string streetSearch = string.Join(" ", searchParts.Take(searchParts.Length - (isNumber ? 1 : 0))).ToLowerInvariant();

                    // Filtrar por rua e, se houver, também pelo número
                    filteredHouses = [.. houses.Where(h =>
                    {
                        bool matchesStreet = string.IsNullOrEmpty(streetSearch) || (h.Rua?.ToLowerInvariant().Contains(streetSearch) ?? false);
                        bool matchesNumber = !isNumber || (h.NumeroCasa != null && h.NumeroCasa == possibleNumber);

                        // Retorna as casas que atendem aos critérios de rua e número
                        return matchesStreet && matchesNumber;
                    })];
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
                var people = await _healthRecordService.GetRecordsByHouseIdAsync(id);

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

        private async Task EditHouseAsync(int id)
        {
            try
            {
                var house = await _houseService.GetHouseByIdAsync(id);

                if (house != null)
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new AddHouse(house));
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private static async Task CreateHouseAsync()
        {
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(new AddHouse());
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private static async Task OpenFamilyPageAsync(int id)
        {
            try
            {
                await Shell.Current.GoToAsync("families", new Dictionary<string, object> { { "id", id } });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }
    }
}