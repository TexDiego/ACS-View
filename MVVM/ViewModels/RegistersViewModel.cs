using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public partial class RegistersViewModel : BaseViewModel
    {
        private HouseService _houseService;
        private readonly HealthRecordService _healthRecordService;
        private List<HealthRecord> _healthRecords = [];
        private readonly ObservableCollection<Pessoa> _person = [];
        public ObservableCollection<Pessoa> Person => _person;
        public ObservableCollection<HealthRecord> _healthRecord { get; private set; }
        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }

        public RegistersViewModel() { }

        public RegistersViewModel(
            HealthRecordService healthRecordService,
            DatabaseService databaseService,
            string condition,
            string search,
            string filter,
            string order)
        {
            _healthRecordService = healthRecordService ?? throw new ArgumentNullException(nameof(healthRecordService));
            _healthRecord = [];
            _houseService = new HouseService(databaseService);

            DeleteCommand = new Command<string>(async susNumber =>
            {
                try
                {
                    bool result = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Confirmar", "Tem certeza de que deseja excluir o cadastro?\n\nSUS: " + susNumber, true, "Excluir", true, "Cancelar")));
                    if (result) return;

                    var record = _healthRecords.FirstOrDefault(r => r.SusNumber == susNumber);
                    if (record != null)
                    {
                        // Exclusão do banco de dados
                        await _healthRecordService.DeleteRecordAsync(record.SusNumber);

                        // Remover da lista interna (sem vínculo com a UI)
                        _healthRecords.Remove(record);

                        // Remover da ObservableCollection que está vinculada à UI
                        var personToRemove = _person.FirstOrDefault(p => p.Sus == susNumber);
                        if (personToRemove != null)
                        {
                            _person.Remove(personToRemove);
                        }

                        // Atualiza a lista visível na UI
                        await UpdateDatasAsync(condition, search, filter, order);
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", $"Não foi possível deletar o registro.\n\n{ex.Message}", true, "Voltar", false, ""));
                }
            });

            EditCommand = new Command<string>(async (susNumber) =>
            {
                var record = _healthRecords.FirstOrDefault(r => r.SusNumber == susNumber);

                if (record != null)
                {
                    // Navegar para a página de edição e passar os dados do registro
                    await Application.Current.MainPage.Navigation.PushAsync(new AddRegister(record, databaseService, record.HouseId, record.FamilyId));
                }
            });

            Task.Run(async () => await LoadHealthRecordsAndUpdateDatasAsync(condition, search, filter, order)).Wait();
        }

        private async Task LoadHealthRecordsAndUpdateDatasAsync(string condition, string search, string filter, string order)
        {
            try
            {
                _healthRecords = await _healthRecordService.GetAllRecordsAsync();
                await UpdateDatasAsync(condition, search, filter, order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar registros: {ex.Message}");
            }
        }

        public async Task UpdateDatasAsync(string condition, string search, string filter, string order)
        {
            try
            {
                // Obter registros filtrados
                var filteredRecords = FilterRecords(condition, search, filter, order);

                var updatedList = new List<Pessoa>();

                foreach (var record in filteredRecords)
                {
                    var endereco = await GetAddressAsync(record.SusNumber);

                    updatedList.Add(new Pessoa
                    {
                        Nome = record.Name,
                        Sus = record.SusNumber,
                        Observacao = record.Observacao,
                        HasObs = record.HasObs,
                        Nascimento = record.BirthDate,
                        Idade = CalcularIdadeCompleta(record.BirthDate),
                        Endereco = endereco,
                    });
                }

                // Atualizar coleção principal na thread da UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _person.Clear();

                    foreach (var pessoa in updatedList)
                        _person.Add(pessoa);

                    OnPropertyChanged(nameof(Person));
                });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "Voltar");
            }
        }


        public static string CalcularIdadeCompleta(DateTime dataNascimento)
        {
            DateTime hoje = DateTime.Today;

            int anos = hoje.Year - dataNascimento.Year;
            int meses = hoje.Month - dataNascimento.Month;
            int dias = hoje.Day - dataNascimento.Day;

            // Ajustar meses e dias, se necessário
            if (dias < 0)
            {
                meses--;
                dias += DateTime.DaysInMonth(hoje.AddMonths(-1).Year, hoje.AddMonths(-1).Month);
            }

            if (meses < 0)
            {
                anos--;
                meses += 12;
            }

            // Casos especiais
            if (anos == 0 && meses == 0 && dias == 0)
                return "Recém-nascido";

            if (anos == 0 && meses == 0)
                return dias == 1 ? "1 dia" : $"{dias} dias";

            if (anos == 0)
                return meses == 1 ? "1 mês" : $"{meses} meses";

            if (anos == 1)
                return meses == 0 ? "1 ano" : meses == 1 ? "1 ano e 1 mês" : $"1 ano e {meses} meses";

            return $"{anos} anos";
        }

        private async Task<string> GetAddressAsync(string sus)
        {
            try
            {
                var house = await _houseService.GetHouseBySusAsync(sus);

                if (house == null)
                    return "Sem endereço.";

                // Tratamento para campos possivelmente nulos
                string rua = house.Rua ?? "";
                string numeroRua = house.NumeroCasa ?? "";
                string complemento = string.IsNullOrWhiteSpace(house.Complemento) ? "" : $"- {house.Complemento}";

                // Construir o endereço completo
                return $"{rua}, {numeroRua} {complemento}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter endereço: {ex.Message}");
                return "Erro ao buscar endereço.";
            }
        }


        private List<HealthRecord> FilterRecords(string condition, string search, string filter, string order)
        {
            try
            {
                // Aplicar filtro inicial baseado em condição
                var filteredRecords = condition switch
                {
                    "GESTANTE" => _healthRecords.Where(r => r.IsPregnant),
                    "DB" => _healthRecords.Where(r => r.HasDiabetes),
                    "HAS" => _healthRecords.Where(r => r.HasHypertension),
                    "HASDB" => _healthRecords.Where(r => r.IsDiabetesAndHypertension),
                    "TB" => _healthRecords.Where(r => r.HasTuberculosis),
                    "HAN" => _healthRecords.Where(r => r.HasLeprosy),
                    "ACAMADO" => _healthRecords.Where(r => r.IsBedridden),
                    "DOMICILIADO" => _healthRecords.Where(r => r.IsHomebound),
                    "MENOR" => _healthRecords.Where(r => r.IsBaby),
                    "MENTAL" => _healthRecords.Where(r => r.HasMentalIllness),
                    "DEFICIENTE" => _healthRecords.Where(r => r.HasDisabilities),
                    "FUMANTE" => _healthRecords.Where(r => r.IsSmoker),
                    "CANCER" => _healthRecords.Where(r => r.HasCancer),
                    "IDOSO" => _healthRecords.Where(r => r.IsOld),
                    _ => _healthRecords,
                };

                // Normalizar e aplicar o termo de pesquisa
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string normalizedSearch = search.Trim().ToLowerInvariant();
                    filteredRecords = filteredRecords.Where(r =>
                        (r.Name?.ToLowerInvariant().Contains(normalizedSearch) ?? false) ||
                        (r.SusNumber?.Contains(normalizedSearch) ?? false));
                }

                // Aplicar ordenação
                var sortedRecords = filter switch
                {
                    "Nome" => order == "Crescente"
                        ? filteredRecords.OrderBy(r => r.Name).ToList()
                        : filteredRecords.OrderByDescending(r => r.Name).ToList(),

                    "Idade" => order == "Decrescente"
                        ? filteredRecords.OrderBy(r => r.BirthDate).ToList()
                        : filteredRecords.OrderByDescending(r => r.BirthDate).ToList(),

                    _ => filteredRecords.ToList()
                };

                return sortedRecords;
            }
            catch (Exception ex)
            {
                // Log para identificar possíveis problemas
                Console.WriteLine($"Erro no filtro: {ex.Message}");
                return new List<HealthRecord>();
            }
        }
    }
}