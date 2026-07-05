using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class AllVisitsViewModel : BaseViewModel
{
    private readonly IVisitsService visitsService;
    private int loadedVersion = -1;
    private bool hasLoaded;

    [ObservableProperty] private ObservableCollection<VisitOverviewMetricCard> metrics = [];
    [ObservableProperty] private ObservableCollection<VisitSuggestionDto> suggestionsPreview = [];
    [ObservableProperty] private bool hasSuggestions;

    public ICommand OpenRecordsCommand { get; }
    public ICommand OpenSuggestionsCommand { get; }
    public ICommand GoToHouseCommand { get; }

    public AllVisitsViewModel(IVisitsService visitsService)
    {
        this.visitsService = visitsService;
        OpenRecordsCommand = new Command(async () => await NavigateToAsync("visitrecords"));
        OpenSuggestionsCommand = new Command(async () => await NavigateToAsync("visitsuggestions"));
        GoToHouseCommand = new Command<int>(async id => await GoToHouse(id));
    }

    internal async Task LoadVisitsAsync(bool force = false)
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
                var overview = await visitsService.GetMonthlyOverviewAsync(DateTime.Today);
                Metrics = new ObservableCollection<VisitOverviewMetricCard>(BuildMetrics(overview));

                var suggestions = await visitsService.GetVisitSuggestionsAsync(DateTime.Today);
                SuggestionsPreview = new ObservableCollection<VisitSuggestionDto>(suggestions.Take(5));
                HasSuggestions = SuggestionsPreview.Count > 0;

                loadedVersion = currentVersion;
                hasLoaded = true;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar visitas: {ex.Message}");
            await DisplayAlertAsync("Erro", "Não foi possível carregar as visitas.");
        }
    }

    private async Task GoToHouse(int id)
    {
        if (id <= 0)
        {
            return;
        }

        await NavigateToAsync("families", new Dictionary<string, object> { { "id", id } });
    }

    private static IEnumerable<VisitOverviewMetricCard> BuildMetrics(VisitMonthlyOverviewDto overview)
    {
        yield return new("Total", overview.TotalVisits.ToString());
        yield return new("Realizadas", overview.CompletedVisits.ToString());
        yield return new("Ausentes", overview.AbsentVisits.ToString());
        yield return new("Recusadas", overview.RefusedVisits.ToString());
        yield return new("Crianças", overview.ChildVisits.ToString());
        yield return new("Gestantes/puerperas", overview.PregnancyPostpartumVisits.ToString());
        yield return new("Hipertensão", overview.HypertensionVisits.ToString());
        yield return new("Diabetes", overview.DiabetesVisits.ToString());
        yield return new("Idosos", overview.ElderlyVisits.ToString());
        yield return new("Benefícios", overview.BenefitVisits.ToString());
    }
}

public sealed record VisitOverviewMetricCard(string Title, string Value);
