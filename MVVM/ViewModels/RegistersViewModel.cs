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
        private readonly HouseService _houseService;
        private readonly HealthRecordService _healthRecordService;
        private readonly VaccineService _vaccineService;
        private readonly DatabaseService databaseService;
        private Vaccines _vaccines;

        public string ScrollToSusNumber { get; set; }

        private readonly ObservableCollection<HealthRecord> _healthRecords = [];
        public ObservableCollection<HealthRecord> HealthRecords => _healthRecords;

        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand PersonInfo { get; }
        public ICommand Vaccines { get; }

        public RegistersViewModel() { }

        public RegistersViewModel(
            HealthRecordService healthRecordService,
            VaccineService vaccineService,
            DatabaseService databaseService,
            string condition,
            string search,
            string filter,
            string order)
        {
            try
            {
                _healthRecordService = healthRecordService ?? throw new ArgumentNullException(nameof(healthRecordService));
                _vaccineService = vaccineService ?? throw new ArgumentNullException(nameof(vaccineService));
                this.databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
                _houseService = new HouseService(databaseService);

                DeleteCommand = new Command<string>(async susNumber => await DeleteRecordAsync(susNumber));
                EditCommand = new Command<string>(async susNumber => await EditRecordAsync(susNumber));
                PersonInfo = new Command<string>(async susNumber => await PersonData(susNumber));
                Vaccines = new Command<string>(async susNumber => await VaccinesPage(susNumber));
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        public async Task LoadHealthRecordsAndUpdateDatasAsync(string condition, string search, string filter, string order)
        {
            try
            {
                var records = await _healthRecordService.GetAllRecordsAsync();
                var filteredRecords = FilterRecords(records, condition, search, filter, order);

                var addressTasks = filteredRecords.Select(async record =>
                {
                    record.Endereco = await GetAddressAsync(record.SusNumber);
                    return record;
                });

                var recordsWithAddress = await Task.WhenAll(addressTasks);


                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _healthRecords.Clear();
                    foreach (var record in filteredRecords)
                    {
                        _healthRecords.Add(record);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar registros: {ex.Message}");
            }
        }

        public async Task InitAsync(string condition, string search, string filter, string order)
        {
            await LoadHealthRecordsAndUpdateDatasAsync(condition, search, filter, order);
        }

        private async Task DeleteRecordAsync(string susNumber)
        {
            try
            {
                bool result = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp(
                    "Confirmar",
                    $"Tem certeza de que deseja excluir o cadastro?\n\nSUS: {susNumber}",
                    true, "Excluir", true, "Cancelar")));

                if (result) return;

                var record = _healthRecords.FirstOrDefault(r => r.SusNumber == susNumber);
                if (record != null)
                {
                    await _healthRecordService.DeleteRecordAsync(record.SusNumber);
                    _healthRecords.Remove(record);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp(
                    "Erro",
                    $"Não foi possível deletar o registro.\n\n{ex.Message}",
                    true, "Voltar", false, ""));
            }
        }

        private async Task EditRecordAsync(string susNumber)
        {
            try
            {
                var record = _healthRecords.FirstOrDefault(r => r.SusNumber == susNumber);

                if (record != null)
                {
                    ScrollToSusNumber = record.SusNumber;
                    await Application.Current.MainPage.Navigation.PushAsync(new AddRegister(record, databaseService, record.HouseId, record.FamilyId));
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private List<HealthRecord> FilterRecords(IEnumerable<HealthRecord> records, string condition, string search, string filter, string order)
        {
            try
            {
                var filteredRecords = records.AsEnumerable();

                // Aplicar condição
                if (!string.IsNullOrEmpty(condition))
                {
                    filteredRecords = condition switch
                    {
                        "GESTANTE" => filteredRecords.Where(r => r.IsPregnant),
                        "DB" => filteredRecords.Where(r => r.HasDiabetes),
                        "HAS" => filteredRecords.Where(r => r.HasHypertension),
                        "HASDB" => filteredRecords.Where(r => r.IsDiabetesAndHypertension),
                        "HAN" => filteredRecords.Where(r => r.HasLeprosy),
                        "TB" => filteredRecords.Where(r => r.HasTuberculosis),
                        "ACAMADO" => filteredRecords.Where(r => r.IsBedridden),
                        "DOMICILIADO" => filteredRecords.Where(r => r.IsHomebound),
                        "BOLSA" => filteredRecords.Where(r => r.BolsaFamilia),
                        "CORACAO" => filteredRecords.Where(r => r.HasHeartDesease),
                        "FIGADO" => filteredRecords.Where(r => r.HasLiverDesease),
                        "RIM" => filteredRecords.Where(r => r.HasKidneyDesease),
                        "PULMAO" => filteredRecords.Where(r => r.HasLungsDesease),
                        "NEURO" => filteredRecords.Where(r => r.IsNeurodivergent),
                        "HIV" => filteredRecords.Where(r => r.HasHIV),
                        "DROGAS" => filteredRecords.Where(r => r.IsDrugAddicted),
                        "MENOR" => filteredRecords.Where(r => r.IsBaby),
                        "MENTAL" => filteredRecords.Where(r => r.HasMentalIllness),
                        "FUMANTE" => filteredRecords.Where(r => r.IsSmoker),
                        "ALCOOLATRA" => filteredRecords.Where(r => r.IsAlcoholic),
                        "DEFICIENTE" => filteredRecords.Where(r => r.HasDisabilities),
                        "CANCER" => filteredRecords.Where(r => r.HasCancer),
                        "IDOSO" => filteredRecords.Where(r => r.IsOld),
                        "NOHOME" => filteredRecords.Where(r => r.HouseId == 0),
                        _ => filteredRecords
                    };
                }

                // Aplicar busca
                if (!string.IsNullOrEmpty(search))
                {
                    string normalizedSearch = search.Trim().ToLowerInvariant();
                    filteredRecords = filteredRecords.Where(r =>
                        (r.Name?.ToLowerInvariant().Contains(normalizedSearch) ?? false) ||
                        (r.SusNumber?.Contains(normalizedSearch) ?? false));
                }

                // Ordenar resultados
                filteredRecords = filter switch
                {
                    "Nome" => order == "Crescente"
                        ? filteredRecords.OrderBy(r => r.Name)
                        : filteredRecords.OrderByDescending(r => r.Name),

                    "Idade" => order == "Crescente"
                        ? filteredRecords.OrderByDescending(r => r.BirthDate)
                        : filteredRecords.OrderBy(r => r.BirthDate),

                    _ => filteredRecords
                };

                return filteredRecords.ToList();
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
                return [];
            }
        }

        private async Task<string> GetAddressAsync(string sus)
        {
            try
            {
                var house = await _houseService.GetHouseBySusAsync(sus);
                if (house == null) return "Sem endereço.";

                string rua = house.Rua ?? "";
                string numeroRua = house.NumeroCasa ?? "";
                string complemento = string.IsNullOrWhiteSpace(house.Complemento) ? "" : $"- {house.Complemento}";

                return $"{rua}, {numeroRua} {complemento}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter endereço: {ex.Message}");
                return "Erro ao buscar endereço.";
            }
        }

        private async Task PersonData(string susNumber)
        {
            try
            {
                var record = await _healthRecordService.GetRecordBySusAsync(susNumber);

                if (record != null)
                {
                    var popup = new PersonsInfo(record, databaseService);
                    await popup.LoadAddressAsync();
                    await Application.Current.MainPage.ShowPopupAsync(popup);
                }
            }
            catch
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", "Erro ao carregar os dados da pessoa.", true, "Fechar", false, ""));
            }
        }

        private async Task VaccinesPage(string susNumber)
        {
            try
            {
                _vaccines = await _vaccineService.GetVaccinesBySusAsync(susNumber);

                if (_vaccines == null)
                {
                    await AddVaccineMissing(susNumber);
                    Console.WriteLine("Adicionando página de vacinas ao cadastro antigo.");

                    _vaccines = await _vaccineService.GetVaccinesBySusAsync(susNumber);
                }

                ScrollToSusNumber = _vaccines.SusNumber;
                await Application.Current.MainPage.Navigation.PushAsync(new VaccinesPage(_vaccines, _vaccineService, databaseService));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", "Erro ao carregar os dados da pessoa.", true, "Fechar", false, ""));
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task AddVaccineMissing(string susNumber)
        {
            try
            {
                var record = await _healthRecordService.GetRecordBySusAsync(susNumber);

                var vaccine = new Vaccines
                {
                    SusNumber = susNumber,
                    BirthDate = record.BirthDate
                };

                await _vaccineService.AdicionarVacinasAsync(vaccine);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Fechar", false, ""));
            }
        }
    }
}
