using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public class HousesPageViewModel : BaseViewModel
    {
        private FamiliesViewModel _familyViewModel;
        private FamilyService _familyService;
        private readonly HealthRecordService healthRecordService;
        private readonly HouseService _houseService;
        private List<House> _houses = [];
        private readonly ObservableCollection<Casa> _casa = [];
        public ObservableCollection<Casa> Casa => _casa;
        public ObservableCollection<House> houses { get; private set; }
        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand FamilyCommand { get; }

        public HousesPageViewModel() { }

        public HousesPageViewModel(DatabaseService databaseService, HouseService houseService, string search, string order)
        {
            _houseService = houseService ?? throw new ArgumentNullException(nameof(houseService));
            healthRecordService = new HealthRecordService(databaseService);
            houses = [];

            DeleteCommand = new Command<int>(async id =>
            {
                try
                {
                    bool result = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Confirmar", "Tem certeza de que deseja excluir a residência?\n\nTodas as famílias desta casa serão perdidas.", true, "Excluir", true, "Cancelar")));
                    if (result) return;

                    Console.WriteLine(id);

                    var record = _houses.FirstOrDefault(r => r.CasaId == id);

                    if (record != null)
                    {
                        var people = await healthRecordService.GetRecordsByHouseID(id);

                        foreach (var person in people)
                        {
                            person.HouseId = 0;
                            person.FamilyId = 0;

                            healthRecordService.UpdateRecordAsync(person);
                        }

                        // Exclusão do banco de dados
                        await _houseService.DeleteHouseAsync(record.CasaId);

                        // Remover da lista interna (sem vínculo com a UI)
                        _houses.Remove(record);

                        // Remover da ObservableCollection que está vinculada à UI
                        var houseToRemove = _casa.FirstOrDefault(p => p.Id == id);
                        if (houseToRemove != null)
                        {
                            _casa.Remove(houseToRemove);
                        }

                        // Atualiza a lista visível na UI
                        UpdateDatas(search, order);
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", $"Não foi possível deletar a residência.\n\n{ex.Message}", true, "Voltar", false, ""));
                }
            });

            EditCommand = new Command<int>(async id =>
            {
                try
                {
                    var record = _houses.FirstOrDefault(r => r.CasaId == id);

                    if (record != null)
                    {
                        await Application.Current.MainPage.Navigation.PushAsync(new AddHouse(record, databaseService, id));
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", $"Não foi possível editar a residência.\n\n{ex.Message}", true, "Voltar", false, ""));
                }
            });

            FamilyCommand = new Command<int>(async id =>
            {
                try
                {
                    Console.WriteLine($"\nFamilyCommand chamado! IDHouse: {id}\n");
                    _familyService = new FamilyService(databaseService);
                    _familyViewModel = new FamiliesViewModel(id);

                    await Application.Current.MainPage.Navigation.PushAsync(new FamiliesPage(_familyViewModel));
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", $"Não foi possível editar a residência.\n\n{ex.Message}", true, "Voltar", false, ""));
                }
            });

            Task.Run(async () => await LoadHealthRecordsAndUpdateDatasAsync(search, order)).Wait();
        }

        private async Task LoadHealthRecordsAndUpdateDatasAsync(string search, string order)
        {
            try
            {
                _houses = await _houseService.GetAllHousesAsync();
                UpdateDatas(search, order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar registros: {ex.Message}");
            }
        }

        public void UpdateDatas(string search, string order)
        {
            try
            {
                var filteredRecords = FilterRecords(search, order);

                _casa.Clear();

                foreach (var record in filteredRecords)
                {
                    _casa.Add(new Casa
                    {
                        Id = record.CasaId,
                        Rua = record.Rua,
                        Complemento = record.Complemento,
                        Numero = record.NumeroCasa,
                        PossuiComplemento = record.PossuiComplemento
                    });
                }

                OnPropertyChanged(nameof(Casa));
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "Voltar");
            }
        }

        private List<House> FilterRecords(string search, string order)
        {
            try
            {
                // Normalizar e aplicar o termo de pesquisa
                var normalizedSearch = search?.Trim().ToLowerInvariant();
                var filteredRecords = string.IsNullOrWhiteSpace(normalizedSearch)
                    ? _houses
                    : (_houses.Where(r => r.Rua?.ToLowerInvariant().Contains(normalizedSearch) ?? false));

                // Função auxiliar para extrair números de uma string
                static int ExtractNumber(string input)
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return int.MaxValue; // Valor padrão para casos sem números

                    var match = System.Text.RegularExpressions.Regex.Match(input, @"\d+");
                    return match.Success ? int.Parse(match.Value) : int.MaxValue;
                }

                // Aplicar ordenação
                filteredRecords = order switch
                {
                    "Crescente" => filteredRecords
                        .OrderBy(r => r.Rua)
                        .ThenBy(r => int.TryParse(r.NumeroCasa, out var numero) ? numero : int.MaxValue)
                        .ThenBy(r => ExtractNumber(r.Complemento))
                        .ThenBy(r => r.Complemento ?? string.Empty), // Ordenar alfabéticamente quando não há números
                    "Decrescente" => filteredRecords
                        .OrderByDescending(r => r.Rua)
                        .ThenByDescending(r => int.TryParse(r.NumeroCasa, out var numero) ? numero : int.MinValue)
                        .ThenByDescending(r => ExtractNumber(r.Complemento))
                        .ThenByDescending(r => r.Complemento ?? string.Empty), // Ordenar alfabéticamente quando não há números
                    _ => filteredRecords
                };

                // Retornar a lista ordenada
                var result = filteredRecords.ToList();
                Console.WriteLine(result);
                return result;
            }
            catch (Exception ex)
            {
                // Log de erro
                Console.WriteLine($"Erro no filtro: {ex.Message}");
                return [];
            }
        }
    }
}