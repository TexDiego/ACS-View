using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class OverallViewModel : BaseViewModel
{
    private readonly IDashboardMetricsService dash;

    private readonly GeneralMetrics generalMetrics = new();
    private readonly HealthMetrics healthMetrics = new();
    private readonly List<MetricCombination> metricCombinations = [];
    private readonly List<Dashboard> removedGeneralRootMetrics = [];
    private readonly List<Dashboard> removedHealthRootMetrics = [];
    private readonly HashSet<string> removedGeneralRootFilterKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> removedHealthRootFilterKeys = new(StringComparer.OrdinalIgnoreCase);
    private int _loadedMetricsVersion = -1;

    [ObservableProperty] private ContentView currentView;

    [ObservableProperty] private bool generalTabSelected = true;
    [ObservableProperty] private bool healthTabSelected;
    [ObservableProperty] private ObservableCollection<Dashboard> healthDashboard = [];
    [ObservableProperty] private ObservableCollection<Dashboard> metricsDashboard = [];
    public ICommand GoToPageAsync => new Command<string>(async (p) => await GoToPage(p));
    public ICommand LoadSummaryCommand => new Command(async () => await LoadSummaryAsync());
    public ICommand SwitchView => new Command<string>(SwitchMetricsView);
    public ICommand AddCombinedMetricCommand => new Command(async () => await AddCombinedMetricAsync());
    public ICommand RemoveMetricCommand => new Command<Dashboard>(RemoveMetric);

    public OverallViewModel(IDashboardMetricsService _dash)
    {
        dash = _dash;
        CurrentView = generalMetrics;
        generalMetrics.BindingContext = this;
        healthMetrics.BindingContext = this;
    }

    private async Task LoadSummaryAsync()
    {
        if (MetricsDashboard.Count > 0 &&
            HealthDashboard.Count > 0 &&
            _loadedMetricsVersion == DataChangeTracker.MetricsVersion)
        {
            return;
        }

        try
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                HealthDashboard.Clear();
                MetricsDashboard.Clear();

                var metrics = await dash.GetMetricsAsync();

                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Pacientes",
                    FilterKey = DashboardFilterKeys.All,
                    Total = metrics.TotalPacientes
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Residências",
                    FilterKey = DashboardFilterKeys.Residences,
                    Total = metrics.TotalResidencias
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Pacientes Sem Residência",
                    FilterKey = DashboardFilterKeys.NoHouse,
                    Total = metrics.TotalSemResidencia
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Pacientes Sem Família",
                    FilterKey = DashboardFilterKeys.NoFamily,
                    Total = metrics.TotalSemFamilia
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Bolsa Família",
                    FilterKey = DashboardFilterKeys.BolsaFamilia,
                    Total = metrics.TotalBolsaFamilia
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Idosos",
                    FilterKey = DashboardFilterKeys.Elderly,
                    Total = metrics.TotalIdosos
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Crianças menores de 6",
                    FilterKey = DashboardFilterKeys.ChildrenUnder6,
                    Total = metrics.TotalCriancasMenoresDe6
                });

                foreach (var condition in await dash.GetConditionsAsync())
                {
                    HealthDashboard.Add(new Dashboard()
                    {
                        Name = condition.Description,
                        FilterKey = $"{DashboardFilterKeys.ConditionPrefix}{condition.Description}",
                        Total = condition.Quantity
                    });
                }

                foreach (var CidMetrics in await dash.GetCidMetricsAsync())
                {
                    HealthDashboard.Add(new Dashboard()
                    {
                        Name = $"{CidMetrics.Cid} - {CidMetrics.Description}",
                        Cid = CidMetrics.Cid,
                        FilterKey = $"{DashboardFilterKeys.CidPrefix}{CidMetrics.Cid}",
                        Total = CidMetrics.Quantity
                    });
                }

                ApplyRemovedRootMetrics(MetricsDashboard, removedGeneralRootFilterKeys, removedGeneralRootMetrics);
                ApplyRemovedRootMetrics(HealthDashboard, removedHealthRootFilterKeys, removedHealthRootMetrics);
                await RestoreCombinedMetricsAsync();
                _loadedMetricsVersion = DataChangeTracker.MetricsVersion;
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", "Não foi possível carregar os dados", "Voltar");
            Debug.WriteLine(ex.StackTrace);
        }
    }

    private async Task GoToPage(string condition)
    {
        if (condition == DashboardFilterKeys.Residences)
        {
            await NavigateToAsync("//houses");
            return;
        }

        await NavigateToAsync("//registers", new Dictionary<string, object> { { "condition", condition } });
    }

    private void SwitchMetricsView(string page)
    {
        CurrentView = page switch
        {
            "0" => generalMetrics,
            "1" => healthMetrics,
            _ => healthMetrics
        };

        GeneralTabSelected = page == "0";
        HealthTabSelected = page == "1";
    }

    private async Task AddCombinedMetricAsync()
    {
        var targetDashboard = HealthTabSelected ? HealthDashboard : MetricsDashboard;
        var removedRootMetrics = GetRemovedRootMetrics();
        var candidates = GetCombinationCandidates(targetDashboard).ToList();
        var canCreateCombination = HasValidCombination(candidates);

        if (removedRootMetrics.Count > 0)
        {
            if (!canCreateCombination)
            {
                await RestoreRemovedRootMetricAsync(targetDashboard, removedRootMetrics);
                return;
            }

            var action = await DisplayActionSheetAsync(
                "Adicionar métrica",
                "Cancelar",
                null,
                "Combinar métricas",
                "Restaurar métrica removida");

            if (action == "Restaurar métrica removida")
            {
                await RestoreRemovedRootMetricAsync(targetDashboard, removedRootMetrics);
                return;
            }

            if (action != "Combinar métricas")
            {
                return;
            }
        }

        if (!canCreateCombination)
        {
            await DisplayAlertAsync("Métricas", "Não há métricas compatíveis para combinar.", "Voltar");
            return;
        }

        var first = await PickMetricAsync("Primeira métrica", candidates);
        if (first is null)
        {
            return;
        }

        var secondCandidates = candidates
            .Where(metric => !string.Equals(metric.FilterKey, first.FilterKey, StringComparison.OrdinalIgnoreCase))
            .Where(metric => CanCombine(first, metric))
            .ToList();

        if (secondCandidates.Count == 0)
        {
            await DisplayAlertAsync("Métricas", "Não há uma segunda métrica compatível com a seleção.", "Voltar");
            return;
        }

        var second = await PickMetricAsync("Segunda métrica", secondCandidates);
        if (second is null)
        {
            return;
        }

        var combinationKey = DashboardFilterKeys.CreateCombination(first.FilterKey, second.FilterKey);
        if (targetDashboard.Any(metric => string.Equals(metric.FilterKey, combinationKey, StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlertAsync("Métricas", "Essa combinação já foi adicionada.", "Voltar");
            return;
        }

        var total = await dash.CountPatientsByFilterAsync(combinationKey);
        var combinedMetric = CreateCombinedDashboard(first, second, combinationKey, total);

        targetDashboard.Add(combinedMetric);
        metricCombinations.Add(new MetricCombination(
            IsHealth: HealthTabSelected,
            FirstFilterKey: first.FilterKey,
            SecondFilterKey: second.FilterKey));
    }

    private IEnumerable<Dashboard> GetCombinationCandidates(IEnumerable<Dashboard> dashboard)
    {
        var candidates = dashboard.Where(metric => !metric.IsCombination);

        if (HealthTabSelected)
        {
            return candidates;
        }

        var allowedGeneralFilters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DashboardFilterKeys.Elderly,
            DashboardFilterKeys.BolsaFamilia,
            DashboardFilterKeys.ChildrenUnder6,
            DashboardFilterKeys.NoHouse,
            DashboardFilterKeys.NoFamily
        };

        return candidates.Where(metric => allowedGeneralFilters.Contains(metric.FilterKey));
    }

    private static bool HasValidCombination(IReadOnlyList<Dashboard> candidates)
    {
        for (var firstIndex = 0; firstIndex < candidates.Count; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < candidates.Count; secondIndex++)
            {
                if (CanCombine(candidates[firstIndex], candidates[secondIndex]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool CanCombine(Dashboard first, Dashboard second)
    {
        var hasNoHouse = string.Equals(first.FilterKey, DashboardFilterKeys.NoHouse, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(second.FilterKey, DashboardFilterKeys.NoHouse, StringComparison.OrdinalIgnoreCase);
        var hasNoFamily = string.Equals(first.FilterKey, DashboardFilterKeys.NoFamily, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(second.FilterKey, DashboardFilterKeys.NoFamily, StringComparison.OrdinalIgnoreCase);
        var hasElderly = string.Equals(first.FilterKey, DashboardFilterKeys.Elderly, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(second.FilterKey, DashboardFilterKeys.Elderly, StringComparison.OrdinalIgnoreCase);
        var hasChildrenUnder6 = string.Equals(first.FilterKey, DashboardFilterKeys.ChildrenUnder6, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(second.FilterKey, DashboardFilterKeys.ChildrenUnder6, StringComparison.OrdinalIgnoreCase);

        return !(hasNoHouse && hasNoFamily) &&
               !(hasElderly && hasChildrenUnder6);
    }

    private static async Task<Dashboard?> PickMetricAsync(string title, IReadOnlyList<Dashboard> metrics)
    {
        var options = metrics
            .Select((metric, index) => new
            {
                Metric = metric,
                Label = $"{index + 1}. {metric.Name}"
            })
            .ToList();

        var selected = await DisplayActionSheetAsync(title, "Cancelar", null, options.Select(option => option.Label).ToArray());
        if (string.IsNullOrWhiteSpace(selected) || selected == "Cancelar")
        {
            return null;
        }

        return options.FirstOrDefault(option => option.Label == selected)?.Metric;
    }

    private async Task RestoreRemovedRootMetricAsync(
        ObservableCollection<Dashboard> targetDashboard,
        List<Dashboard> removedRootMetrics)
    {
        var metric = await PickMetricAsync("Restaurar métrica", removedRootMetrics);
        if (metric is null)
        {
            return;
        }

        removedRootMetrics.RemoveAll(item =>
            string.Equals(item.FilterKey, metric.FilterKey, StringComparison.OrdinalIgnoreCase));

        GetRemovedRootFilterKeys().Remove(metric.FilterKey);
        AddRootMetric(targetDashboard, metric);
    }

    private void RemoveMetric(Dashboard? metric)
    {
        if (metric is null)
        {
            return;
        }

        var isHealthMetric = HealthDashboard.Contains(metric);
        var targetDashboard = isHealthMetric ? HealthDashboard : MetricsDashboard;

        if (!targetDashboard.Remove(metric))
        {
            return;
        }

        if (metric.IsCombination)
        {
            RemoveCombinationRecord(isHealthMetric, metric.FilterKey);
            return;
        }

        var removedRootMetrics = isHealthMetric ? removedHealthRootMetrics : removedGeneralRootMetrics;
        var removedRootFilterKeys = isHealthMetric ? removedHealthRootFilterKeys : removedGeneralRootFilterKeys;

        removedRootMetrics.RemoveAll(item =>
            string.Equals(item.FilterKey, metric.FilterKey, StringComparison.OrdinalIgnoreCase));
        removedRootMetrics.Add(metric);
        removedRootFilterKeys.Add(metric.FilterKey);
        RemoveDependentCombinations(targetDashboard, isHealthMetric, metric.FilterKey);
    }

    private static void AddRootMetric(ObservableCollection<Dashboard> dashboard, Dashboard metric)
    {
        var firstCombinationIndex = dashboard
            .Select((item, index) => new { item, index })
            .FirstOrDefault(pair => pair.item.IsCombination)
            ?.index;

        if (firstCombinationIndex is null)
        {
            dashboard.Add(metric);
            return;
        }

        dashboard.Insert(firstCombinationIndex.Value, metric);
    }

    private void RemoveDependentCombinations(
        ObservableCollection<Dashboard> dashboard,
        bool isHealthMetric,
        string rootFilterKey)
    {
        var dependentCombinations = dashboard
            .Where(metric =>
                metric.IsCombination &&
                DashboardFilterKeys.GetCombinationParts(metric.FilterKey)
                    .Any(part => string.Equals(part, rootFilterKey, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var combination in dependentCombinations)
        {
            dashboard.Remove(combination);
            RemoveCombinationRecord(isHealthMetric, combination.FilterKey);
        }
    }

    private void RemoveCombinationRecord(bool isHealthMetric, string combinationFilterKey)
    {
        metricCombinations.RemoveAll(combination =>
            combination.IsHealth == isHealthMetric &&
            string.Equals(
                DashboardFilterKeys.CreateCombination(combination.FirstFilterKey, combination.SecondFilterKey),
                combinationFilterKey,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyRemovedRootMetrics(
        ObservableCollection<Dashboard> dashboard,
        HashSet<string> removedRootFilterKeys,
        List<Dashboard> removedRootMetrics)
    {
        removedRootMetrics.Clear();

        foreach (var metric in dashboard
                     .Where(metric =>
                         !metric.IsCombination &&
                         removedRootFilterKeys.Contains(metric.FilterKey))
                     .ToList())
        {
            dashboard.Remove(metric);
            removedRootMetrics.Add(metric);
        }
    }

    private List<Dashboard> GetRemovedRootMetrics()
    {
        return HealthTabSelected ? removedHealthRootMetrics : removedGeneralRootMetrics;
    }

    private HashSet<string> GetRemovedRootFilterKeys()
    {
        return HealthTabSelected ? removedHealthRootFilterKeys : removedGeneralRootFilterKeys;
    }

    private async Task RestoreCombinedMetricsAsync()
    {
        foreach (var combination in metricCombinations)
        {
            var targetDashboard = combination.IsHealth ? HealthDashboard : MetricsDashboard;
            var first = targetDashboard.FirstOrDefault(metric =>
                !metric.IsCombination &&
                string.Equals(metric.FilterKey, combination.FirstFilterKey, StringComparison.OrdinalIgnoreCase));
            var second = targetDashboard.FirstOrDefault(metric =>
                !metric.IsCombination &&
                string.Equals(metric.FilterKey, combination.SecondFilterKey, StringComparison.OrdinalIgnoreCase));

            if (first is null || second is null)
            {
                continue;
            }

            var combinationKey = DashboardFilterKeys.CreateCombination(first.FilterKey, second.FilterKey);
            var total = await dash.CountPatientsByFilterAsync(combinationKey);
            targetDashboard.Add(CreateCombinedDashboard(first, second, combinationKey, total));
        }
    }

    private static Dashboard CreateCombinedDashboard(Dashboard first, Dashboard second, string combinationKey, int total)
    {
        return new Dashboard
        {
            Name = $"{first.Name} + {second.Name}",
            FilterKey = combinationKey,
            Total = total,
            IsCombination = true
        };
    }

    private sealed record MetricCombination(bool IsHealth, string FirstFilterKey, string SecondFilterKey);
}
