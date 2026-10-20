using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class VetorialControlViewModel(IHouseService houseService) : BaseViewModel
{
    private int loadedVersion = -1;
    private int sessionVersion = -1;
    private int loadVersion;
    private string loadedSearch = "";
    private int loadedSearchMode;
    private CancellationTokenSource? debounce;
    private List<House>? territorialHouses;
    private int territorialVersion = -1;
    private int territorialSession = -1;
    [ObservableProperty] private IReadOnlyList<VectorControlRow> items = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyMessage))]
    private string searchText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchPlaceholder))]
    private int searchModeIndex = 0;

    public string[] SearchModes { get; } = ["Endereço ou paciente", "Setor censitário", "Quadra"];
    public string SearchPlaceholder => SearchModeIndex switch
    {
        1 => "Buscar setor censitário",
        2 => "Buscar quadra",
        _ => "Buscar rua, número ou paciente"
    };
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchText)
        ? "Nenhuma residência cadastrada."
        : "Nenhuma residência encontrada.";

    partial void OnSearchTextChanged(string value) => ScheduleSearch();
    partial void OnSearchModeIndexChanged(int value) => ScheduleSearch();

    private void ScheduleSearch()
    {
        debounce?.Cancel(); debounce?.Dispose();
        debounce = new CancellationTokenSource();
        // Invalidate an in-flight request as soon as the criteria change.
        Interlocked.Increment(ref loadVersion);
        _ = DebounceSearchAsync(debounce.Token);
    }

    private async Task DebounceSearchAsync(CancellationToken token)
    {
        try { await Task.Delay(300, token); await LoadAsync(); }
        catch (OperationCanceledException) { }
    }

    public void CancelPendingSearch()
    {
        debounce?.Cancel(); debounce?.Dispose(); debounce = null;
        Interlocked.Increment(ref loadVersion);
        IsLoading = false;
    }

    private Task SearchAsync()
    {
        debounce?.Cancel();
        return LoadAsync();
    }

    public ICommand SearchCommand => new RelayCommand(async () => await SearchAsync());
    public ICommand OptionsCommand => new RelayCommand(async () => await NavigateToAsync("vetorialcontroloptions"));
    public ICommand FamilyCommand => new RelayCommand<VectorControlRow>(async row => await NavigateToAsync("families", new Dictionary<string, object> { { "id", row.HouseId } }));

    public async Task LoadAsync()
    {
        var query = SearchText?.Trim() ?? "";
        var mode = Math.Clamp(SearchModeIndex, 0, 2);

        if (loadedVersion == DataChangeTracker.HousesVersion && sessionVersion == DataChangeTracker.SessionVersion
            && loadedSearch == query && loadedSearchMode == mode)
        {
            IsLoading = false;
            return;
        }

        if (sessionVersion != DataChangeTracker.SessionVersion) Items = Array.Empty<VectorControlRow>();

        var request = Interlocked.Increment(ref loadVersion);
        IsLoading = true;
        var version = DataChangeTracker.HousesVersion;
        var session = DataChangeTracker.SessionVersion;

        try
        {
            List<House> houses;

            if (mode != 0 && territorialHouses != null && territorialVersion == version && territorialSession == session)
                houses = territorialHouses;
            else
            {
                houses = await houseService.GetTerritorialHousesAsync(mode == 0 ? query : null);
                if (mode != 0 && request == loadVersion && session == DataChangeTracker.SessionVersion)
                {
                    territorialHouses = houses;
                    territorialVersion = version;
                    territorialSession = session;
                }
            }

            var rows = await Task.Run(() =>
            {
                var normalized = SearchTextNormalizer.Normalize(query);

                var matches = mode == 0 || normalized.Length == 0 ? houses : houses.Where(h =>
                    SearchTextNormalizer.Normalize(mode == 1 ? h.SetorCensitario : h.NumeroQuadra).Contains(normalized, StringComparison.Ordinal));

                return VectorControlHierarchy.Build(matches);
            });

            if (request != loadVersion) return;

            if (session != DataChangeTracker.SessionVersion)
            {
                Items = Array.Empty<VectorControlRow>();
                return;
            }

            Items = rows;
            loadedVersion = version; sessionVersion = session;
            loadedSearch = query; loadedSearchMode = mode;
        }
        catch (Exception ex)
        {
            if (request == loadVersion) await DisplayAlertAsync("Erro", ex.Message);
        }
        finally
        {
            if (request == loadVersion) IsLoading = false;
        }
    }
}
