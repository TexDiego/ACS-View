using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.UseCases.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class HousesPageViewModel(IHouseService _houseService, IPatientService _patientService) : BaseViewModel
    {
        private const int PageSize = 30;
        private const int SearchDebounceMilliseconds = 300;

        private CancellationTokenSource? _throttleCts;
        private bool _hasMoreHouses = true;
        private int _loadedHousesCount;
        private int _loadVersion;
        private int _loadedHousesVersion = -1;
        private string _lastLoadedSearch = string.Empty;

        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private ObservableCollection<HouseListItemDto> houses = [];
        [ObservableProperty] private int totalHouses;
        [ObservableProperty] private bool isLoadingMore;

        public ObservableCollection<int> SkeletonItems { get; } = new([1, 2, 3, 4, 5, 6, 7, 8]);

        public ICommand DeleteCommand => new Command<int>(async id => await DeleteHouseAsync(id));
        public ICommand EditCommand => new Command<HouseListItemDto>(async house => await EditHouseAsync(house));
        public ICommand FamilyCommand => new Command<int>(async id => await NavigateToAsync("families", new Dictionary<string, object> { { "id", id } }));
        public ICommand LoadHousesCommand => new Command<string?>(async search => await SearchHousesAsync(search));
        public ICommand LoadMoreHousesCommand => new Command(async () => await LoadMoreHousesAsync());
        public ICommand NewHouseCommand => new Command(async () => await NavigateToAsync("addhouse", new Dictionary<string, object> { { "house", new House() } }));

        public async Task LoadInitialHousesAsync()
        {
            var normalizedSearch = SearchText?.Trim() ?? string.Empty;
            if (Houses.Count > 0 &&
                _loadedHousesVersion == DataChangeTracker.HousesVersion &&
                string.Equals(_lastLoadedSearch, normalizedSearch, StringComparison.Ordinal))
            {
                return;
            }

            await RefreshHousesAsync(SearchText);
        }

        private async Task SearchHousesAsync(string? search)
        {
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
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message);
            }
        }

        private async Task RefreshHousesAsync(string? search)
        {
            var normalizedSearch = search?.Trim() ?? string.Empty;
            var loadVersion = Interlocked.Increment(ref _loadVersion);

            IsLoading = true;
            IsLoadingMore = false;
            _hasMoreHouses = true;
            _loadedHousesCount = 0;

            try
            {
                var result = await _houseService.GetHouseListAsync(search, 0, PageSize);
                if (loadVersion != _loadVersion)
                {
                    return;
                }

                var firstPage = new ObservableCollection<HouseListItemDto>(result.Items);

                RunOnMainThread(() =>
                {
                    Houses = firstPage;
                    TotalHouses = result.TotalCount;
                    _loadedHousesCount = Houses.Count;
                    _hasMoreHouses = _loadedHousesCount < TotalHouses;
                    _loadedHousesVersion = DataChangeTracker.HousesVersion;
                    _lastLoadedSearch = normalizedSearch;
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

        private async Task LoadMoreHousesAsync()
        {
            if (IsLoading || IsLoadingMore || !_hasMoreHouses)
            {
                return;
            }

            IsLoadingMore = true;

            try
            {
                var result = await _houseService.GetHouseListAsync(SearchText, _loadedHousesCount, PageSize);

                RunOnMainThread(() =>
                {
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
                IsLoadingMore = false;
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
                        person.HouseId = 0;
                        person.FamilyId = 0;
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
    }
}
