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

    [ObservableProperty] private ObservableCollection<VisitOverviewMetricCard> metrics = [];
    [ObservableProperty] private ObservableCollection<VisitSuggestionDto> suggestionsPreview = [];
    [ObservableProperty] private bool hasSuggestions;

    public ICommand OpenRecordsCommand => new Command(async () => await NavigateToAsync("visitrecords"));
    public ICommand OpenSuggestionsCommand => new Command(async () => await NavigateToAsync("visitsuggestions"));
    public ICommand GoToHouseCommand => new Command<int>(async id => await GoToHouse(id));
    public ICommand Refresh => new Command(async () => await LoadVisitsAsync());

    public AllVisitsViewModel(IVisitsService visitsService)
    {
        this.visitsService = visitsService;
    }

    internal async Task LoadVisitsAsync(bool force = false)
    {
        IsLoading = true;

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

            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar visitas: {ex.Message}");
            await DisplayAlertAsync("Erro", "Não foi possível carregar as visitas.");
        }
        finally
        {
            IsLoading = false;
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
        yield return new("Registros: Crianças", overview.ChildVisits.ToString());
        yield return new("Registros: Gestantes/puerperas", overview.PregnancyPostpartumVisits.ToString());
        yield return new("Registros: Hipertensão", overview.HypertensionVisits.ToString());
        yield return new("Registros: Diabetes", overview.DiabetesVisits.ToString());
        yield return new("Registros: Idosos", overview.ElderlyVisits.ToString());
        yield return new("Registros: Benefícios", overview.BenefitVisits.ToString());
    }
}

public sealed record VisitOverviewMetricCard(string Title, string Value);
