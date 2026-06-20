using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.UseCases.DTOs;
using ACS_View.Views;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class RegistersViewModel(IVaccineService _vaccineService, IHouseService _houseService, IPatientService _patientService) : BaseViewModel
    {
        private const int PageSize = 30;
        private const int SearchDebounceMilliseconds = 300;

        private CancellationTokenSource? _searchCts;
        private bool _hasMorePatients = true;
        private int _loadedPatientsCount;
        private int _loadVersion;
        private int _loadedPatientsVersion = -1;
        private string _lastLoadedSearch = string.Empty;
        private string _lastLoadedFilter = string.Empty;
        private string _filterKey = "ALL";
        private PatientListFilterDto _listFilter = new();
        private DateTime _suppressReloadUntilUtc = DateTime.MinValue;

        [ObservableProperty] private string condition = "Pacientes";
        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private ObservableCollection<PatientListItemDto> patients = [];
        [ObservableProperty] private int totalRecords;
        [ObservableProperty] private bool isLoadingMore;
        [ObservableProperty] private bool hasActiveFilters;
        [ObservableProperty] private string filterSummary = string.Empty;

        private Vaccines? _vaccines;

        public int ScrollToId { get; set; }
        public ObservableCollection<int> SkeletonItems { get; } = new([1, 2, 3, 4, 5, 6, 7, 8]);

        public ICommand DeleteCommand => new Command<int>(async (id) => await DeleteRecordAsync(id));
        public ICommand EditCommand => new Command<PatientListItemDto>(async (p) => await EditRecordAsync(p));
        public ICommand PersonInfo => new Command<PatientListItemDto>(async record => await PersonData(record));
        public ICommand Vaccines => new Command<int>(async (id) => await VaccinesPage(id));
        public ICommand AddPerson => new Command(async () => await NavigateToAsync("addregister"));
        public ICommand Houses => new Command(async () => await NavigateToAsync("//houses"));
        public ICommand Filter => new Command(async () => await ShowFilterPopupAsync());
        public ICommand ClearFiltersCommand => new Command(async () => await ClearFiltersAsync());
        public ICommand SearchPatientsCommand => new Command<string?>(async search => await SearchPatientsAsync(search));
        public ICommand LoadMorePatientsCommand => new Command(async () => await LoadMorePatientsAsync());

        internal async Task LoadPatients()
        {
            var normalizedSearch = SearchText?.Trim() ?? string.Empty;
            if (Patients.Count > 0 &&
                _loadedPatientsVersion == DataChangeTracker.PatientsVersion &&
                string.Equals(_lastLoadedSearch, normalizedSearch, StringComparison.Ordinal) &&
                string.Equals(_lastLoadedFilter, _filterKey, StringComparison.Ordinal))
            {
                return;
            }

            await RefreshPatientsAsync(SearchText, useDebounce: false);
        }

        internal void SetFilter(string filterKey)
        {
            _filterKey = string.IsNullOrWhiteSpace(filterKey) ? "ALL" : filterKey;
            _listFilter.FilterKey = _filterKey;
            Condition = GetFilterTitle(_filterKey);
            UpdateFilterState();
        }

        internal bool ShouldSkipTransientReload()
        {
            return DateTime.UtcNow <= _suppressReloadUntilUtc;
        }

        private async Task SearchPatientsAsync(string? search)
        {
            var cancellationToken = ResetSearchDebounce();

            try
            {
                await Task.Delay(SearchDebounceMilliseconds, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await RefreshPatientsAsync(search, useDebounce: false);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private async Task RefreshPatientsAsync(string? search, bool useDebounce)
        {
            var normalizedSearch = search?.Trim() ?? string.Empty;
            var loadVersion = Interlocked.Increment(ref _loadVersion);

            if (useDebounce)
            {
                await Task.Delay(SearchDebounceMilliseconds);
            }

            IsLoading = true;
            IsLoadingMore = false;
            _hasMorePatients = true;
            _loadedPatientsCount = 0;

            try
            {
                var result = await _patientService.GetPatientListAsync(normalizedSearch, 0, PageSize, _listFilter);
                if (loadVersion != _loadVersion)
                {
                    return;
                }

                var firstPage = new ObservableCollection<PatientListItemDto>(result.Items);

                RunOnMainThread(() =>
                {
                    Patients = firstPage;
                    TotalRecords = result.TotalCount;
                    _loadedPatientsCount = Patients.Count;
                    _hasMorePatients = _loadedPatientsCount < TotalRecords;
                    _loadedPatientsVersion = DataChangeTracker.PatientsVersion;
                    _lastLoadedSearch = normalizedSearch;
                    _lastLoadedFilter = _filterKey;
                });
            }
            finally
            {
                if (loadVersion == _loadVersion)
                {
                    IsLoading = false;
                }
            }
        }

        private async Task LoadMorePatientsAsync()
        {
            if (IsLoading || IsLoadingMore || !_hasMorePatients)
            {
                return;
            }

            IsLoadingMore = true;

            try
            {
                var result = await _patientService.GetPatientListAsync(SearchText, _loadedPatientsCount, PageSize, _listFilter);

                RunOnMainThread(() =>
                {
                    foreach (var patient in result.Items)
                    {
                        Patients.Add(patient);
                    }

                    TotalRecords = result.TotalCount;
                    _loadedPatientsCount = Patients.Count;
                    _hasMorePatients = _loadedPatientsCount < TotalRecords;
                });
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        private async Task<string> GetAddressAsync(int id)
        {
            try
            {
                var house = await _houseService.GetHouseByPatientIdAsync(id);
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

        private async Task DeleteRecordAsync(int id)
        {
            var confirm = await DisplayConfirmationAsync(
                "Confirmar",
                $"Tem certeza de que deseja excluir o cadastro?",
                "Excluir");

            if (!confirm) return;

            var record = Patients.FirstOrDefault(r => r.Id == id);
            if (record != null)
            {
                await _patientService.DeletePatient(id);
                Patients.Remove(record);
                _loadedPatientsCount = Patients.Count;
                TotalRecords = Math.Max(0, TotalRecords - 1);
                _hasMorePatients = _loadedPatientsCount < TotalRecords;
            }
        }

        private async Task EditRecordAsync(PatientListItemDto patientListItem)
        {
            ScrollToId = patientListItem.Id;
            await NavigateToAsync("addregister", new Dictionary<string, object> { { "patientId", patientListItem.Id } });
        }

        private async Task PersonData(PatientListItemDto record)
        {
            if (record == null) return;

            var patient = await _patientService.GetPatientById(record.Id);

            if (patient == null)
            {
                await DisplayAlertAsync("Erro", "Cadastro não encontrado.", "Voltar");
                return;
            }

            PersonsInfoViewModel vm = App.StaticServiceProvider.GetService<PersonsInfoViewModel>();
            var popup = new PersonsInfo(vm);

            popup.SetPatient(patient);
            SuppressTransientReload();
            try
            {
                await Shell.Current.ShowPopupAsync(popup, PopupConfigs.Default);
            }
            finally
            {
                SuppressTransientReload();
            }
        }

        private async Task VaccinesPage(int id)
        {
            ScrollToId = id;
            await NavigateToAsync("vaccines", new Dictionary<string, object> { { "patientId", id } });
        }

        private async Task ShowFilterPopupAsync()
        {
            var popup = new FilterPopup(_listFilter.Clone());
            var popupResult = await Shell.Current.ShowPopupAsync<PatientListFilterDto>(popup, PopupConfigs.Default);

            if (popupResult.WasDismissedByTappingOutsideOfPopup ||
                popupResult.Result is null)
            {
                return;
            }

            _listFilter = popupResult.Result;
            _filterKey = _listFilter.FilterKey;
            Condition = GetFilterTitle(_filterKey);
            UpdateFilterState();

            await RefreshPatientsAsync(SearchText, useDebounce: false);
        }

        private async Task ClearFiltersAsync()
        {
            _searchCts?.Cancel();
            SearchText = string.Empty;
            _filterKey = "ALL";
            _listFilter = new PatientListFilterDto();
            Condition = GetFilterTitle(_filterKey);
            UpdateFilterState();

            await RefreshPatientsAsync(SearchText, useDebounce: false);
        }

        private CancellationToken ResetSearchDebounce()
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();

            return _searchCts.Token;
        }

        private void SuppressTransientReload()
        {
            _suppressReloadUntilUtc = DateTime.UtcNow.AddSeconds(2);
        }

        partial void OnSearchTextChanged(string value)
        {
            UpdateFilterState();
        }

        private void UpdateFilterState()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                parts.Add("busca");
            }

            if (!string.Equals(_filterKey, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(GetFilterTitle(_filterKey));
            }

            if (_listFilter.MinimumAge.HasValue || _listFilter.MaximumAge.HasValue)
            {
                parts.Add(GetAgeSummary());
            }

            if (_listFilter.SortBy != PatientListSortOption.Name || _listFilter.SortDescending)
            {
                parts.Add(_listFilter.SortBy == PatientListSortOption.Age ? "idade" : "nome desc.");
            }

            HasActiveFilters = parts.Count > 0;
            FilterSummary = parts.Count == 0 ? string.Empty : $"Filtros: {string.Join(", ", parts)}";
        }

        private string GetAgeSummary()
        {
            return (_listFilter.MinimumAge, _listFilter.MaximumAge) switch
            {
                (not null, not null) => $"{_listFilter.MinimumAge}-{_listFilter.MaximumAge} anos",
                (not null, null) => $"a partir de {_listFilter.MinimumAge} anos",
                (null, not null) => $"até {_listFilter.MaximumAge} anos",
                _ => string.Empty
            };
        }

        private static string GetFilterTitle(string filterKey)
        {
            if (DashboardFilterKeys.IsCombination(filterKey))
            {
                return string.Join(" + ", DashboardFilterKeys.GetCombinationParts(filterKey).Select(GetFilterTitle));
            }

            if (filterKey.StartsWith(DashboardFilterKeys.ConditionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return filterKey[DashboardFilterKeys.ConditionPrefix.Length..];
            }

            if (filterKey.StartsWith(DashboardFilterKeys.CidPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return $"CID {filterKey[DashboardFilterKeys.CidPrefix.Length..]}";
            }

            return filterKey switch
            {
                "NO_HOUSE" => "Pacientes Sem Residência",
                "NO_FAMILY" => "Pacientes Sem Família",
                "BOLSA_FAMILIA" => "Bolsa Família",
                "ELDERLY" => "Idosos",
                "CHILDREN_UNDER_6" => "Crianças menores de 6",
                _ => "Pacientes"
            };
        }
    }
}
