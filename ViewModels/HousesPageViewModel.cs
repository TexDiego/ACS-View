using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.Application.DTOs;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class HousesPageViewModel(IHouseService _houseService, IPatientService _patientService, IPopupService _popupService, IDialogService _dialogService) : BaseViewModel
    {
        private const int PageSize = 30;
        private const int SearchDebounceMilliseconds = 300;

        private CancellationTokenSource? _throttleCts;
        private bool _hasMoreHouses = true;
        private int _loadedHousesCount;
        private int _loadVersion;
        private int _loadedHousesVersion = -1;
        private string _lastLoadedSearch = string.Empty;
        private string _filterKey = DashboardFilterKeys.All;
        private string _lastLoadedFilterKey = DashboardFilterKeys.All;

        private HouseNumberFilterDto _numberFilter = new();
        private HouseNumberFilterDto? _lastLoadedNumberFilter;

        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private ObservableCollection<HouseListItemDto> houses = [];
        [ObservableProperty] private int totalHouses;
        [ObservableProperty] private bool isLoadingMore;
        [ObservableProperty] private bool hasActiveFilters;
        [ObservableProperty] private string filterSummary = string.Empty;

        public ObservableCollection<int> SkeletonItems { get; } = new([1, 2, 3, 4, 5, 6, 7, 8]);

        public ICommand DeleteCommand => new Command<int>(async id => await DeleteHouseAsync(id));
        public ICommand EditCommand => new Command<HouseListItemDto>(async house => await EditHouseAsync(house));
        public ICommand FamilyCommand => new Command<int>(async id => await NavigateToAsync("families", new Dictionary<string, object> { { "id", id } }));
        public ICommand LoadHousesCommand => new Command<string?>(async search => await SearchHousesAsync(search));
        public ICommand LoadMoreHousesCommand => new Command(async () => await LoadMoreHousesAsync());
        public ICommand NewHouseCommand => new Command(async () => await NavigateToAsync("addhouse", new Dictionary<string, object> { { "house", new House() } }));
        public ICommand VetorialControlCommand => new Command(async () => await NavigateToAsync("vetorialcontrol"));
        public ICommand Filter => new Command(async () => await ShowFilterPopupAsync());
        public ICommand ClearFiltersCommand => new Command(async () => await ClearFiltersAsync());

        public async Task LoadInitialHousesAsync()
        {
            var normalizedSearch = SearchText?.Trim() ?? string.Empty;
            if (Houses.Count > 0 &&
                _loadedHousesVersion == DataChangeTracker.HousesVersion &&
                string.Equals(_lastLoadedSearch, normalizedSearch, StringComparison.Ordinal) &&
                string.Equals(_lastLoadedFilterKey, _filterKey, StringComparison.OrdinalIgnoreCase) &&
                _lastLoadedNumberFilter == _numberFilter)
            {
                return;
            }

            await RefreshHousesAsync(SearchText);
        }

        public void SetFilter(string? filterKey)
        {
            _filterKey = DashboardFilterKeys.GetBaseFilterKey(filterKey);
            Interlocked.Increment(ref _loadVersion);
            UpdateFilterState();
        }

        private async Task SearchHousesAsync(string? search)
        {
            Interlocked.Increment(ref _loadVersion);
            IsLoading = true;
            IsLoadingMore = false;
            try
            {
                var cancellationToken = ResetSearchDebounce();
                await Task.Delay(SearchDebounceMilliseconds, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                await RefreshHousesAsync(search);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex) when (IsCancellationException(ex))
            {
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message);
            }
        }

        private async Task RefreshHousesAsync(string? search)
        {
            var normalizedSearch = search?.Trim() ?? string.Empty;
            var loadVersion = Interlocked.Increment(ref _loadVersion);
            var filterKey = _filterKey;
            var numberFilter = _numberFilter;

            IsLoading = true;
            IsLoadingMore = false;
            _hasMoreHouses = true;
            _loadedHousesCount = 0;

            try
            {
                var result = await _houseService.GetHouseListAsync(normalizedSearch, 0, PageSize, filterKey, numberFilter);
                if (loadVersion != _loadVersion)
                {
                    return;
                }

                var firstPage = new ObservableCollection<HouseListItemDto>(result.Items);

                RunOnMainThread(() =>
                {
                    if (loadVersion != _loadVersion) return;
                    Houses = firstPage;
                    TotalHouses = result.TotalCount;
                    _loadedHousesCount = Houses.Count;
                    _hasMoreHouses = _loadedHousesCount < TotalHouses;
                    _loadedHousesVersion = DataChangeTracker.HousesVersion;
                    _lastLoadedSearch = normalizedSearch;
                    _lastLoadedFilterKey = filterKey;
                    _lastLoadedNumberFilter = numberFilter;
                });
            }
            catch (Exception ex) when (IsCancellationException(ex))
            {
            }
            finally
            {
                if (loadVersion == _loadVersion)
                {
                    IsLoading = false;
                }
            }
        }

        private async Task LoadMoreHousesAsync()
        {
            if (IsLoading || IsLoadingMore || !_hasMoreHouses)
            {
                return;
            }

            IsLoadingMore = true;
            var loadVersion = _loadVersion;

            try
            {
                var result = await _houseService.GetHouseListAsync(SearchText, _loadedHousesCount, PageSize, _filterKey, _numberFilter);
                if (loadVersion != _loadVersion) return;

                RunOnMainThread(() =>
                {
                    if (loadVersion != _loadVersion) return;
                    foreach (var house in result.Items)
                    {
                        Houses.Add(house);
                    }

                    TotalHouses = result.TotalCount;
                    _loadedHousesCount = Houses.Count;
                    _hasMoreHouses = _loadedHousesCount < TotalHouses;
                });
            }
            finally
            {
                if (loadVersion == _loadVersion) IsLoadingMore = false;
            }
        }

        private async Task DeleteHouseAsync(int id)
        {
            try
            {
                var confirm = await DisplayConfirmationAsync(
                    "Confirmar",
                    "Tem certeza de que deseja excluir a residência?\n\nTodas as famílias desta casa serão perdidas.",
                    "Excluir"
                );

                if (!confirm)
                {
                    return;
                }

                var people = await _patientService.GetPatientsByHouseId(id);

                if (people != null)
                {
                    foreach (var person in people)
                    {
                        person.HouseId = -1;
                        person.FamilyId = -1;
                        person.FamilyResponsibleSus = null;
                        await _patientService.UpdatePatient(person);
                    }
                }

                await _houseService.DeleteHouseAsync(id);
                await RefreshHousesAsync(SearchText);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private async Task EditHouseAsync(HouseListItemDto houseListItem)
        {
            try
            {
                await NavigateToAsync("addhouse", new Dictionary<string, object> { { "houseId", houseListItem.CasaId } });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private CancellationToken ResetSearchDebounce()
        {
            _throttleCts?.Cancel();
            _throttleCts?.Dispose();
            _throttleCts = new CancellationTokenSource();

            return _throttleCts.Token;
        }

        private async Task ClearFiltersAsync()
        {
            _throttleCts?.Cancel();
            _numberFilter = new();
            SetFilter(DashboardFilterKeys.All);
            await RefreshHousesAsync(SearchText);
        }

        private async Task ShowFilterPopupAsync()
        {
            var result = await _popupService.ShowAsync<HouseNumberFilterDto>(new HouseFilterPopup(_numberFilter, _dialogService));
            if (result.WasDismissed || result.Result is null) return;
            _throttleCts?.Cancel();
            _numberFilter = result.Result;
            UpdateFilterState();
            await RefreshHousesAsync(SearchText);
        }

        private void UpdateFilterState()
        {
            var parts = new List<string>();
            if (!string.Equals(_filterKey, DashboardFilterKeys.All, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_filterKey, DashboardFilterKeys.Residences, StringComparison.OrdinalIgnoreCase))
                parts.Add(GetFilterTitle(_filterKey));
            if (_numberFilter.IsActive) parts.Add(_numberFilter.Summary);
            HasActiveFilters = parts.Count > 0;
            FilterSummary = HasActiveFilters ? $"Filtros: {string.Join(", ", parts)}" : string.Empty;
        }

        private static string GetFilterTitle(string filterKey)
        {
            return filterKey switch
            {
                DashboardFilterKeys.EmptyResidences => "Residências vazias",
                _ => "Filtro aplicado"
            };
        }
    }
}
