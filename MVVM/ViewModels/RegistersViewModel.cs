using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class RegistersViewModel : BaseViewModel
    {
        private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();
        private readonly IHealthRecordFilterService _filterService = App.ServiceProvider.GetRequiredService<IHealthRecordFilterService>();
        private readonly IVaccineService _vaccineService = App.ServiceProvider.GetRequiredService<IVaccineService>();
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();
        private readonly IUserDialogService _dialogService = App.ServiceProvider.GetRequiredService<IUserDialogService>();

        private Vaccines _vaccines;

        public string ScrollToSusNumber { get; set; }

        [ObservableProperty] private ObservableCollection<HealthRecord> healthRecords = [];
        [ObservableProperty] private int totalRecords;

        public ICommand DeleteCommand => new Command<string>(async susNumber => await DeleteRecordAsync(susNumber));
        public ICommand EditCommand => new Command<string>(async susNumber => await EditRecordAsync(susNumber));
        public ICommand PersonInfo => new Command<string>(async susNumber => await PersonData(susNumber));
        public ICommand Vaccines => new Command<string>(async susNumber => await VaccinesPage(susNumber));
        public ICommand AddPerson => new Command(async () => await Shell.Current.GoToAsync("addregister"));
        public ICommand Houses => new Command(async () => await Application.Current.MainPage.Navigation.PushAsync(new HousesPage()));
        public ICommand Filter => new Command(async () => await Application.Current.MainPage.ShowPopupAsync(new FilterPopup()));


        public async Task InitAsync(string condition, string? search, string? filter, string? order)
        {
            await LoadHealthRecordsAndUpdateDatasAsync(condition, search, filter, order);
        }

        private async Task LoadHealthRecordsAndUpdateDatasAsync(string condition, string? search, string? filter, string? order)
        {
            try
            {
                var records = await _healthRecordService.GetAllRecordsAsync();
                var filteredRecords = _filterService.ApplyFilters(records, condition, search, filter, order);

                var addressTasks = filteredRecords.Select(async record =>
                {
                    record.Endereco = await GetAddressAsync(record.SusNumber);
                    return record;
                });

                var recordsWithAddress = await Task.WhenAll(addressTasks);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HealthRecords.Clear();
                    foreach (var record in recordsWithAddress)
                    {
                        HealthRecords.Add(record);
                    }
                });

                TotalRecords = HealthRecords.Count;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowError("Erro ao carregar registros");
                Console.WriteLine(ex.Message);
            }
        }

        private static List<HealthRecord> FilterRecords(IEnumerable<HealthRecord> records, string condition, string search, string filter, string order)
        {
            return new HealthRecordFilterService().ApplyFilters(records, condition, search, filter, order);
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

        private async Task DeleteRecordAsync(string susNumber)
        {
            var confirm = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp(
                                            "Confirmar",
                                            $"Tem certeza de que deseja excluir o cadastro?\n\nSUS: {susNumber}",
                                            true, "Excluir", true, "Cancelar")));

            if (confirm) return;

            var record = HealthRecords.FirstOrDefault(r => r.SusNumber == susNumber);
            if (record != null)
            {
                await _healthRecordService.DeleteRecordAsync(susNumber);
                HealthRecords.Remove(record);
            }

            TotalRecords = HealthRecords.Count;
        }

        private async Task EditRecordAsync(string susNumber)
        {
            var record = HealthRecords.FirstOrDefault(r => r.SusNumber == susNumber);
            if (record == null) return;

            ScrollToSusNumber = record.SusNumber;

            await Shell.Current.GoToAsync("addregister", new Dictionary<string, object> { { "record", record } });
        }

        private async Task PersonData(string susNumber)
        {
            var record = await _healthRecordService.GetRecordBySusAsync(susNumber);
            if (record == null) return;

            var popup = new PersonsInfo(record);

            await popup.LoadAddressAsync();
            await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        private async Task VaccinesPage(string susNumber)
        {
            _vaccines = await _vaccineService.GetVaccinesBySusAsync(susNumber);

            if (_vaccines == null)
            {
                await AddVaccineMissing(susNumber);
                _vaccines = await _vaccineService.GetVaccinesBySusAsync(susNumber);
            }

            ScrollToSusNumber = _vaccines.SusNumber;
            var page = new VaccinesPage(_vaccines);
            await Application.Current.MainPage.Navigation.PushAsync(page);
        }

        private async Task AddVaccineMissing(string susNumber)
        {
            var record = await _healthRecordService.GetRecordBySusAsync(susNumber);
            if (record == null) return;

            var vaccine = new Vaccines
            {
                SusNumber = susNumber,
                BirthDate = record.BirthDate
            };

            await _vaccineService.AdicionarVacinasAsync(vaccine);
        }
    }
}