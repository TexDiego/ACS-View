using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class CIDViewViewModel(ICidRepository cidRepository, IPopupService popupService) : BaseViewModel
{
    private const int PageSize = 50;
    private const int SearchDebounceMilliseconds = 300;
    private CancellationTokenSource? _searchCts;
    private int _loadVersion;
    private int _loadedCount;
    private bool _initialized;

    [ObservableProperty] private ObservableCollection<CidSearchResultDto> items = [];
    [ObservableProperty] private ObservableCollection<CidCodePrefixFilterDto> codePrefixes = BuildPrefixFilters();
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedCodePrefix = "Todos";
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasMoreItems = true;
    [ObservableProperty] private bool hasResults;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string emptyMessage = "Nenhum CID encontrado.";

    public ICommand LoadMoreCommand => new Command(async () => await LoadMoreAsync());
    public ICommand SelectPrefixCommand => new Command<CidCodePrefixFilterDto>(async filter => await SelectPrefixAsync(filter));
    public ICommand OpenDetailsCommand => new Command<CidSearchResultDto>(async item => await OpenDetailsAsync(item));

    public async Task InitializeAsync()
    {
        if (_initialized && Items.Count > 0)
        {
            return;
        }

        _initialized = true;
        await ReloadAsync(useDebounce: false);
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = ReloadAsync(useDebounce: true);
    }

    private async Task SelectPrefixAsync(CidCodePrefixFilterDto? filter)
    {
        if (filter is null ||
            string.Equals(filter.Prefix, SelectedCodePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SelectedCodePrefix = filter.Prefix;
        UpdatePrefixSelection();
        await ReloadAsync(useDebounce: false);
    }

    private async Task ReloadAsync(bool useDebounce)
    {
        var cancellationToken = ResetSearchDebounce();
        var loadVersion = Interlocked.Increment(ref _loadVersion);

        try
        {
            if (useDebounce)
            {
                await Task.Delay(SearchDebounceMilliseconds, cancellationToken);
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsLoading = true;
                IsLoadingMore = false;
                Items.Clear();
                HasResults = false;
                IsEmpty = false;
                EmptyMessage = "Nenhum CID encontrado.";
            });

            _loadedCount = 0;
            HasMoreItems = true;
            await LoadPageAsync(loadVersion, reset: true, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                HasResults = false;
                IsEmpty = true;
                EmptyMessage = "Nao foi possivel carregar os CIDs.";
            });
        }
        finally
        {
            if (loadVersion == _loadVersion)
            {
                IsLoading = false;
                HasResults = Items.Count > 0;
                IsEmpty = Items.Count == 0;
            }
        }
    }

    private async Task LoadMoreAsync()
    {
        if (IsLoading || IsLoadingMore || !HasMoreItems)
        {
            return;
        }

        var loadVersion = _loadVersion;
        IsLoadingMore = true;

        try
        {
            await LoadPageAsync(loadVersion, reset: false, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            HasMoreItems = false;
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private async Task LoadPageAsync(int loadVersion, bool reset, CancellationToken cancellationToken)
    {
        var prefix = string.Equals(SelectedCodePrefix, "Todos", StringComparison.OrdinalIgnoreCase)
            ? null
            : SelectedCodePrefix;

        var results = await cidRepository.SearchCidAsync(
            SearchText,
            prefix,
            reset ? 0 : _loadedCount,
            PageSize,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (loadVersion != _loadVersion)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (reset)
            {
                Items.Clear();
                _loadedCount = 0;
            }

            foreach (var item in results)
            {
                Items.Add(item);
            }

            _loadedCount = Items.Count;
            HasMoreItems = results.Count == PageSize;
            HasResults = Items.Count > 0;
            IsEmpty = !HasResults && !IsLoading;
            EmptyMessage = "Nenhum CID encontrado.";
        });
    }

    private async Task OpenDetailsAsync(CidSearchResultDto? item)
    {
        if (item is null)
        {
            return;
        }

        await popupService.ShowAsync(new CidDetailsPopup(item));
    }

    private CancellationToken ResetSearchDebounce()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        return _searchCts.Token;
    }

    private void UpdatePrefixSelection()
    {
        foreach (var filter in CodePrefixes)
        {
            filter.IsSelected = string.Equals(filter.Prefix, SelectedCodePrefix, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static ObservableCollection<CidCodePrefixFilterDto> BuildPrefixFilters()
    {
        var prefixes = new[]
        {
            "Todos", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K",
            "L", "M", "N", "O", "P", "Q", "R", "S", "T", "V", "W", "X", "Y", "Z"
        };

        return new ObservableCollection<CidCodePrefixFilterDto>(
            prefixes.Select(prefix => new CidCodePrefixFilterDto
            {
                Prefix = prefix,
                DisplayName = prefix,
                IsSelected = prefix == "Todos"
            }));
    }
}
