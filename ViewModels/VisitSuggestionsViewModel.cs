using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class VisitSuggestionsViewModel : BaseViewModel
{
    private readonly IVisitsService visitsService;
    private int loadedVersion = -1;
    private bool hasLoaded;

    [ObservableProperty] private ObservableCollection<VisitSuggestionDto> suggestions = [];
    [ObservableProperty] private string footerText = string.Empty;
    [ObservableProperty] private bool showEmptyView = false;

    public ICommand GoToHouseCommand => new Command<int>(async id => await GoToHouseAsync(id));
    public ICommand Refresh => new Command(async () => await LoadAsync(true));

    public VisitSuggestionsViewModel(IVisitsService visitsService)
    {
        this.visitsService = visitsService;
    }

    internal async Task LoadAsync(bool force = false)
    {
        var currentVersion = DataChangeTracker.VisitsVersion + DataChangeTracker.PatientsVersion + DataChangeTracker.HousesVersion;
        if (!force && hasLoaded && loadedVersion == currentVersion)
        {
            ShowEmptyView = Suggestions.Count == 0;
            return;
        }

        IsLoading = true;
        ShowEmptyView = false;
        try
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                await visitsService.PurgeExpiredVisitsAsync(DateTime.Today);
                var suggestions = await visitsService.GetVisitSuggestionsAsync(DateTime.Today);
                Suggestions = new ObservableCollection<VisitSuggestionDto>(suggestions);
                FooterText = suggestions.Count == 1 ? "1 sugestão pendente" : $"{suggestions.Count} sugestões pendentes";
                loadedVersion = currentVersion;
                hasLoaded = true;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar sugestões de visita: {ex.Message}");
            await DisplayAlertAsync("Erro", "Não foi possível carregar as sugestões de visita.");
        }
        finally
        {
            IsLoading = false;
            ShowEmptyView = Suggestions.Count == 0;
        }
    }

    private async Task GoToHouseAsync(int id)
    {
        if (id <= 0)
        {
            await DisplayAlertAsync("Residência não vinculada", "Esta pessoa ainda não possui residência vinculada para abrir a página de famílias.");
            return;
        }

        await NavigateToAsync("families", new Dictionary<string, object> { { "id", id } });
    }
}
