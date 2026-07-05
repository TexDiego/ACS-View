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

    public ICommand GoToHouseCommand { get; }

    public VisitSuggestionsViewModel(IVisitsService visitsService)
    {
        this.visitsService = visitsService;
        GoToHouseCommand = new Command<int>(async id => await GoToHouseAsync(id));
    }

    internal async Task LoadAsync(bool force = false)
    {
        var currentVersion = DataChangeTracker.VisitsVersion + DataChangeTracker.PatientsVersion + DataChangeTracker.HousesVersion;
        if (!force && hasLoaded && loadedVersion == currentVersion)
        {
            return;
        }

        try
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                await visitsService.PurgeExpiredVisitsAsync(DateTime.Today);
                var suggestions = await visitsService.GetVisitSuggestionsAsync(DateTime.Today);
                Suggestions = new ObservableCollection<VisitSuggestionDto>(suggestions);
                FooterText = suggestions.Count == 1 ? "1 sugestao pendente" : $"{suggestions.Count} sugestoes pendentes";
                loadedVersion = currentVersion;
                hasLoaded = true;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar sugestoes de visita: {ex.Message}");
            await DisplayAlertAsync("Erro", "Nao foi possivel carregar as sugestoes de visita.");
        }
    }

    private async Task GoToHouseAsync(int id)
    {
        if (id <= 0)
        {
            return;
        }

        await NavigateToAsync("families", new Dictionary<string, object> { { "id", id } });
    }
}
