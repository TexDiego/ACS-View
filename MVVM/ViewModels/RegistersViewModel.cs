﻿using ACS_View.MVVM.Models;
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
        private readonly DatabaseService databaseService;

        private readonly ObservableCollection<HealthRecord> _healthRecords = new();
        public ObservableCollection<HealthRecord> HealthRecords => _healthRecords;

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
            this.databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _houseService = new HouseService(databaseService);

            DeleteCommand = new Command<string>(async susNumber => await DeleteRecordAsync(susNumber));
            EditCommand = new Command<string>(async susNumber => await EditRecordAsync(susNumber));

            Task.Run(async () => await LoadHealthRecordsAndUpdateDatasAsync(condition, search, filter, order));
        }

        public async Task LoadHealthRecordsAndUpdateDatasAsync(string condition, string search, string filter, string order)
        {
            try
            {
                var records = await _healthRecordService.GetAllRecordsAsync();
                var filteredRecords = FilterRecords(records, condition, search, filter, order);

                foreach (var record in filteredRecords)
                {
                    record.Endereco = await GetAddressAsync(record.SusNumber);
                }

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
            var record = _healthRecords.FirstOrDefault(r => r.SusNumber == susNumber);

            if (record != null)
            {
                await Application.Current.MainPage.Navigation.PushAsync(new AddRegister(record, databaseService, record.HouseId, record.FamilyId));
            }
        }

        private List<HealthRecord> FilterRecords(IEnumerable<HealthRecord> records, string condition, string search, string filter, string order)
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
                    "MENOR" => filteredRecords.Where(r => r.IsBaby),
                    "MENTAL" => filteredRecords.Where(r => r.HasMentalIllness),
                    "FUMANTE" => filteredRecords.Where(r => r.IsSmoker),
                    "DEFICIENTE" => filteredRecords.Where(r => r.HasDisabilities),
                    "CANCER" => filteredRecords.Where(r => r.HasCancer),
                    "IDOSO" => filteredRecords.Where(r => r.IsOld),
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
    }
}
