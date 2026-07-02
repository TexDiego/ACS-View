using ACS_View.Application.Interfaces;
using ACS_View.Application.DTOs;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class OverallViewModel : BaseViewModel
{
    private readonly IDashboardMetricsService dash;
    private readonly IDashboardMetricPreferencesService metricPreferences;
    private readonly IPopupService popupService;

    private readonly List<MetricCombination> metricCombinations = [];
    private readonly List<Dashboard> removedGeneralRootMetrics = [];
    private readonly List<Dashboard> removedHealthRootMetrics = [];
    private readonly HashSet<string> removedGeneralRootFilterKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> removedHealthRootFilterKeys = new(StringComparer.OrdinalIgnoreCase);
    private int _loadedMetricsVersion = -1;
    private int _loadedSessionVersion = -1;
    private bool hasLoadedMetricPreferences;
    private bool suppressMetricPreferencePersistence;

    [ObservableProperty] private bool generalTabSelected = true;
    [ObservableProperty] private bool healthTabSelected;
    [ObservableProperty] private ObservableCollection<Dashboard> healthDashboard = [];
    [ObservableProperty] private ObservableCollection<Dashboard> metricsDashboard = [];
    public ICommand GoToPageAsync => new Command<string>(async (p) => await GoToPage(p));
    public ICommand LoadSummaryCommand => new Command(async () => await LoadSummaryAsync());
    public ICommand SwitchView => new Command<string>(SwitchMetricsView);
    public ICommand OpenMetricsMenuCommand => new Command(async () => await OpenMetricsMenuAsync());
    public ICommand AddCombinedMetricCommand => new Command(async () => await AddCombinedMetricAsync());
    public ICommand RemoveMetricCommand => new Command<Dashboard>(async metric => await RemoveMetricAsync(metric));
    public ICommand NavigateToCIDView => new Command(async () => await NavigateToAsync("cids"));

    public OverallViewModel(
        IDashboardMetricsService _dash,
        IDashboardMetricPreferencesService metricPreferencesService,
        IPopupService popupService)
    {
        dash = _dash;
        metricPreferences = metricPreferencesService;
        this.popupService = popupService;
        MetricsDashboard.CollectionChanged += OnDashboardCollectionChanged;
        HealthDashboard.CollectionChanged += OnDashboardCollectionChanged;
    }

    private async Task LoadSummaryAsync()
    {
        if (_loadedSessionVersion != DataChangeTracker.SessionVersion)
        {
            ResetMetricPreferenceState();
        }

        if (MetricsDashboard.Count > 0 &&
            HealthDashboard.Count > 0 &&
            _loadedMetricsVersion == DataChangeTracker.MetricsVersion &&
            _loadedSessionVersion == DataChangeTracker.SessionVersion)
        {
            return;
        }

        try
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                suppressMetricPreferencePersistence = true;
                HealthDashboard.Clear();
                MetricsDashboard.Clear();

                var preferences = await metricPreferences.GetAsync();
                ApplyMetricPreferencesToState(preferences);

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
                    Name = "Famílias",
                    FilterKey = DashboardFilterKeys.Families,
                    Total = metrics.TotalFamilias
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Residências vazias",
                    FilterKey = DashboardFilterKeys.EmptyResidences,
                    Total = metrics.TotalResidenciasVazias
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Pacientes Sem Residência",
                    FilterKey = DashboardFilterKeys.NoHouse,
                    Total = metrics.TotalSemResidencia
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
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Mulheres 25 a 64",
                    FilterKey = DashboardFilterKeys.Women25To64,
                    Summary = BuildModifierSummary(new MetricModifierSelection(nameof(Sexo.Feminino), 25, 64), "Resumo geral"),
                    Total = metrics.TotalMulheres25a64
                });
                MetricsDashboard.Add(new Dashboard()
                {
                    Name = "Inativos",
                    FilterKey = DashboardFilterKeys.Inactive,
                    Summary = "Cadastros fora das estatisticas",
                    Total = metrics.TotalInativos
                });

                foreach (var condition in await dash.GetConditionsAsync())
                {
                    HealthDashboard.Add(new Dashboard()
                    {
                        Name = condition.Description,
                        FilterKey = $"{DashboardFilterKeys.ConditionPrefix}{condition.Description}",
                        Summary = "Condição monitorada",
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
                        Summary = "Condição monitorada",
                        Total = CidMetrics.Quantity
                    });
                }

                ApplyRemovedRootMetrics(MetricsDashboard, removedGeneralRootFilterKeys, removedGeneralRootMetrics);
                ApplyRemovedRootMetrics(HealthDashboard, removedHealthRootFilterKeys, removedHealthRootMetrics);
                await RestoreCombinedMetricsAsync();
                ApplyDashboardOrder(MetricsDashboard, preferences.GeneralOrder);
                ApplyDashboardOrder(HealthDashboard, preferences.HealthOrder);
                _loadedMetricsVersion = DataChangeTracker.MetricsVersion;
                _loadedSessionVersion = DataChangeTracker.SessionVersion;
                hasLoadedMetricPreferences = true;
                suppressMetricPreferencePersistence = false;
            });
        }
        catch (Exception ex)
        {
            suppressMetricPreferencePersistence = false;
            await DisplayAlertAsync("Erro", "Não foi possível carregar os dados", "Voltar");
            Debug.WriteLine(ex.StackTrace);
        }
    }

    private async Task GoToPage(string condition)
    {
        if (string.Equals(condition, DashboardFilterKeys.Families, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (condition == DashboardFilterKeys.Residences ||
            condition == DashboardFilterKeys.EmptyResidences)
        {
            await NavigateToAsync("//houses", new Dictionary<string, object>
            {
                { "filterKey", condition },
                { "metricNavigationId", DateTime.UtcNow.Ticks.ToString() }
            });
            return;
        }

        if (condition == DashboardFilterKeys.BolsaFamilia)
        {
            await NavigateToAsync("bolsafamilia");
            return;
        }

        await NavigateToAsync("//registers", new Dictionary<string, object>
        {
            { "condition", condition },
            { "metricNavigationId", DateTime.UtcNow.Ticks.ToString() }
        });
    }

    private void SwitchMetricsView(string page)
    {
        GeneralTabSelected = page == "0";
        HealthTabSelected = page == "1";
    }

    private async Task OpenMetricsMenuAsync()
    {
        var targetDashboard = HealthTabSelected ? HealthDashboard : MetricsDashboard;
        var removedRootMetrics = GetRemovedRootMetrics();
        var candidates = GetCombinationCandidates(targetDashboard).ToList();
        var canCreateCombination = HasValidCombination(candidates);

        var result = await popupService.ShowAsync<OverallMetricMenuAction>(
            new OverallMetricMenuPopup(removedRootMetrics.Count > 0, canCreateCombination));

        if (result.WasDismissed || result is null)
        {
            return;
        }

        switch (result.Result)
        {
            case OverallMetricMenuAction.OpenCids:
                await NavigateToAsync("cids");
                break;
            case OverallMetricMenuAction.RestoreHiddenMetrics:
                await RestoreRemovedRootMetricAsync(targetDashboard, removedRootMetrics);
                break;
            case OverallMetricMenuAction.AddMetric:
                await AddCombinedMetricAsync();
                break;
            default:
                return;
        }
    }

    private async Task AddCombinedMetricAsync()
    {
        var targetDashboard = HealthTabSelected ? HealthDashboard : MetricsDashboard;
        var candidates = GetCombinationCandidates(targetDashboard).ToList();
        var canCreateCombination = HasValidCombination(candidates);

        if (!canCreateCombination)
        {
            await DisplayAlertAsync("Métricas", "Não há métricas compatíveis para combinar.", "Voltar");
            return;
        }

        var requestResult = await popupService.ShowAsync<DashboardMetricCreateRequestDto>(new AddMetricPopup(candidates));
        if (requestResult.WasDismissed || requestResult.Result is null)
        {
            return;
        }

        var request = requestResult.Result;
        var first = candidates.FirstOrDefault(metric =>
            string.Equals(metric.FilterKey, request.FirstFilterKey, StringComparison.OrdinalIgnoreCase));
        var second = candidates.FirstOrDefault(metric =>
            string.Equals(metric.FilterKey, request.SecondFilterKey, StringComparison.OrdinalIgnoreCase));

        if (first is null || second is null)
        {
            await DisplayAlertAsync("Métricas", "Não foi possível localizar as métricas selecionadas.", "Voltar");
            return;
        }

        if (!CanCombine(first, second))
        {
            await DisplayAlertAsync("Métricas", "Essas métricas não podem ser combinadas.", "Voltar");
            return;
        }

        var modifiers = new MetricModifierSelection(
            request.SexModifier,
            request.MinimumAgeModifier,
            request.MaximumAgeModifier);

        var combinationKey = DashboardFilterKeys.CreateModified(
            DashboardFilterKeys.CreateCombination(first.FilterKey, second.FilterKey),
            modifiers.Sex,
            modifiers.MinimumAge,
            modifiers.MaximumAge);

        if (targetDashboard.Any(metric => string.Equals(metric.FilterKey, combinationKey, StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlertAsync("Métricas", "Essa combinação já foi adicionada.", "Voltar");
            return;
        }

        var total = await dash.CountPatientsByFilterAsync(combinationKey);
        var combinedMetric = CreateCombinedDashboard(
            first,
            second,
            combinationKey,
            total,
            BuildModifierSummary(modifiers, GetDefaultSummary(HealthTabSelected)));

        suppressMetricPreferencePersistence = true;

        try
        {
            targetDashboard.Add(combinedMetric);
            metricCombinations.Add(new MetricCombination(
                IsHealth: HealthTabSelected,
                FirstFilterKey: first.FilterKey,
                SecondFilterKey: second.FilterKey,
                SexModifier: modifiers.Sex,
                MinimumAgeModifier: modifiers.MinimumAge,
                MaximumAgeModifier: modifiers.MaximumAge));
        }
        finally
        {
            suppressMetricPreferencePersistence = false;
        }

        await SaveMetricPreferencesAsync();
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
            DashboardFilterKeys.Women25To64,
            DashboardFilterKeys.NoHouse
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
        var hasElderly = string.Equals(first.FilterKey, DashboardFilterKeys.Elderly, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(second.FilterKey, DashboardFilterKeys.Elderly, StringComparison.OrdinalIgnoreCase);
        var hasChildrenUnder6 = string.Equals(first.FilterKey, DashboardFilterKeys.ChildrenUnder6, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(second.FilterKey, DashboardFilterKeys.ChildrenUnder6, StringComparison.OrdinalIgnoreCase);
        var hasWomen25To64 = string.Equals(first.FilterKey, DashboardFilterKeys.Women25To64, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(second.FilterKey, DashboardFilterKeys.Women25To64, StringComparison.OrdinalIgnoreCase);
        var hasInactive = string.Equals(first.FilterKey, DashboardFilterKeys.Inactive, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(second.FilterKey, DashboardFilterKeys.Inactive, StringComparison.OrdinalIgnoreCase);

        return !(hasElderly && hasChildrenUnder6) &&
               !(hasWomen25To64 && hasChildrenUnder6) &&
               !hasInactive;
    }

    private async Task<MetricModifierSelection?> PickMetricModifiersAsync()
    {
        var action = await DisplayActionSheetAsync(
            "Modificadores",
            "Cancelar",
            null,
            "Sem modificadores",
            "Sexo",
            "Idade",
            "Sexo e idade");

        if (string.IsNullOrWhiteSpace(action) || action == "Cancelar")
        {
            return null;
        }

        if (action == "Sem modificadores")
        {
            return new MetricModifierSelection();
        }

        string? sex = null;
        int? minimumAge = null;
        int? maximumAge = null;

        if (action.Contains("Sexo", StringComparison.OrdinalIgnoreCase))
        {
            sex = await PickSexModifierAsync();
            if (sex is null)
            {
                return null;
            }
        }

        if (action.Contains("Idade", StringComparison.OrdinalIgnoreCase))
        {
            var ageModifier = await PickAgeModifierAsync();
            if (ageModifier is null)
            {
                return null;
            }

            minimumAge = ageModifier.MinimumAge;
            maximumAge = ageModifier.MaximumAge;
        }

        return new MetricModifierSelection(sex, minimumAge, maximumAge);
    }

    private async Task<string?> PickSexModifierAsync()
    {
        var selected = await DisplayActionSheetAsync(
            "Sexo",
            "Cancelar",
            null,
            nameof(Sexo.Feminino),
            nameof(Sexo.Masculino),
            nameof(Sexo.Indeterminado));

        return string.IsNullOrWhiteSpace(selected) || selected == "Cancelar"
            ? null
            : selected;
    }

    private async Task<MetricModifierSelection?> PickAgeModifierAsync()
    {
        var minimumAgeText = await DisplayPromptAsync(
            "Idade mínima",
            "Informe a idade mínima ou deixe em branco.",
            placeholder: "Ex.: 25",
            maxLength: 3,
            keyboard: Keyboard.Numeric);

        if (minimumAgeText is null)
        {
            return null;
        }

        var maximumAgeText = await DisplayPromptAsync(
            "Idade máxima",
            "Informe a idade máxima ou deixe em branco.",
            placeholder: "Ex.: 64",
            maxLength: 3,
            keyboard: Keyboard.Numeric);

        if (maximumAgeText is null)
        {
            return null;
        }

        return await ValidateAgeModifierAsync(minimumAgeText, maximumAgeText);
    }

    private async Task<bool> TryParseAgeModifierAsync(string value, string fieldName, Action<int?> setAge)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            setAge(null);
            return true;
        }

        if (!int.TryParse(value.Trim(), out var parsedAge) || parsedAge < 0 || parsedAge > 120)
        {
            await DisplayAlertAsync("Métricas", $"Informe uma {fieldName} válida entre 0 e 120.", "Voltar");
            return false;
        }

        setAge(parsedAge);
        return true;
    }

    private async Task<MetricModifierSelection?> ValidateAgeModifierAsync(string minimumAgeText, string maximumAgeText)
    {
        int? minimumAge = null;
        int? maximumAge = null;

        if (!await TryParseAgeModifierAsync(minimumAgeText, "idade mínima", value => minimumAge = value) ||
            !await TryParseAgeModifierAsync(maximumAgeText, "idade máxima", value => maximumAge = value))
        {
            return null;
        }

        if (!minimumAge.HasValue && !maximumAge.HasValue)
        {
            return new MetricModifierSelection();
        }

        if (minimumAge.HasValue && maximumAge.HasValue && minimumAge > maximumAge)
        {
            await DisplayAlertAsync("Métricas", "A idade mínima não pode ser maior que a idade máxima.", "Voltar");
            return null;
        }

        return new MetricModifierSelection(MinimumAge: minimumAge, MaximumAge: maximumAge);
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
        suppressMetricPreferencePersistence = true;

        try
        {
            AddRootMetric(targetDashboard, metric);
        }
        finally
        {
            suppressMetricPreferencePersistence = false;
        }

        await SaveMetricPreferencesAsync();
    }

    private async Task RemoveMetricAsync(Dashboard? metric)
    {
        if (metric is null)
        {
            return;
        }

        var isHealthMetric = HealthDashboard.Contains(metric);
        var targetDashboard = isHealthMetric ? HealthDashboard : MetricsDashboard;

        suppressMetricPreferencePersistence = true;

        try
        {
            if (!targetDashboard.Remove(metric))
            {
                return;
            }

            if (metric.IsCombination)
            {
                RemoveCombinationRecord(isHealthMetric, metric.FilterKey);
            }
            else
            {
                var removedRootMetrics = isHealthMetric ? removedHealthRootMetrics : removedGeneralRootMetrics;
                var removedRootFilterKeys = isHealthMetric ? removedHealthRootFilterKeys : removedGeneralRootFilterKeys;

                removedRootMetrics.RemoveAll(item =>
                    string.Equals(item.FilterKey, metric.FilterKey, StringComparison.OrdinalIgnoreCase));
                removedRootMetrics.Add(metric);
                removedRootFilterKeys.Add(metric.FilterKey);
                RemoveDependentCombinations(targetDashboard, isHealthMetric, metric.FilterKey);
            }
        }
        finally
        {
            suppressMetricPreferencePersistence = false;
        }

        await SaveMetricPreferencesAsync();
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
                DashboardFilterKeys.CreateModified(
                    DashboardFilterKeys.CreateCombination(combination.FirstFilterKey, combination.SecondFilterKey),
                    combination.SexModifier,
                    combination.MinimumAgeModifier,
                    combination.MaximumAgeModifier),
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

            var modifiers = new MetricModifierSelection(
                combination.SexModifier,
                combination.MinimumAgeModifier,
                combination.MaximumAgeModifier);
            var combinationKey = DashboardFilterKeys.CreateModified(
                DashboardFilterKeys.CreateCombination(first.FilterKey, second.FilterKey),
                modifiers.Sex,
                modifiers.MinimumAge,
                modifiers.MaximumAge);
            var total = await dash.CountPatientsByFilterAsync(combinationKey);
            targetDashboard.Add(CreateCombinedDashboard(
                first,
                second,
                combinationKey,
                total,
                BuildModifierSummary(modifiers, GetDefaultSummary(combination.IsHealth))));
        }
    }

    private static Dashboard CreateCombinedDashboard(Dashboard first, Dashboard second, string combinationKey, int total, string summary)
    {
        return new Dashboard
        {
            Name = $"{first.Name} + {second.Name}",
            FilterKey = combinationKey,
            Summary = summary,
            Total = total,
            IsCombination = true
        };
    }

    private static string GetDefaultSummary(bool isHealth)
    {
        return isHealth ? "Condição monitorada" : "Resumo geral";
    }

    private static string BuildModifierSummary(MetricModifierSelection modifiers, string fallback)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(modifiers.Sex))
        {
            parts.Add(modifiers.Sex);
        }

        var ageSummary = (modifiers.MinimumAge, modifiers.MaximumAge) switch
        {
            (not null, not null) => $"{modifiers.MinimumAge} a {modifiers.MaximumAge} anos",
            (not null, null) => $"a partir de {modifiers.MinimumAge} anos",
            (null, not null) => $"até {modifiers.MaximumAge} anos",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(ageSummary))
        {
            parts.Add(ageSummary);
        }

        return parts.Count == 0 ? fallback : string.Join(" · ", parts);
    }

    private async void OnDashboardCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (suppressMetricPreferencePersistence || !hasLoadedMetricPreferences)
        {
            return;
        }

        try
        {
            await SaveMetricPreferencesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void ResetMetricPreferenceState()
    {
        suppressMetricPreferencePersistence = true;
        hasLoadedMetricPreferences = false;
        _loadedMetricsVersion = -1;
        metricCombinations.Clear();
        removedGeneralRootMetrics.Clear();
        removedHealthRootMetrics.Clear();
        removedGeneralRootFilterKeys.Clear();
        removedHealthRootFilterKeys.Clear();
        MetricsDashboard.Clear();
        HealthDashboard.Clear();
        suppressMetricPreferencePersistence = false;
    }

    private void ApplyMetricPreferencesToState(DashboardMetricPreferencesDto preferences)
    {
        metricCombinations.Clear();
        removedGeneralRootFilterKeys.Clear();
        removedHealthRootFilterKeys.Clear();

        foreach (var filterKey in preferences.RemovedGeneralRootFilterKeys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            removedGeneralRootFilterKeys.Add(filterKey);
        }

        foreach (var filterKey in preferences.RemovedHealthRootFilterKeys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            removedHealthRootFilterKeys.Add(filterKey);
        }

        foreach (var combination in preferences.Combinations)
        {
            if (string.IsNullOrWhiteSpace(combination.FirstFilterKey) ||
                string.IsNullOrWhiteSpace(combination.SecondFilterKey))
            {
                continue;
            }

            metricCombinations.Add(new MetricCombination(
                combination.IsHealth,
                combination.FirstFilterKey,
                combination.SecondFilterKey,
                combination.SexModifier,
                combination.MinimumAgeModifier,
                combination.MaximumAgeModifier));
        }
    }

    private static void ApplyDashboardOrder(ObservableCollection<Dashboard> dashboard, IReadOnlyList<string> persistedOrder)
    {
        if (dashboard.Count <= 1 || persistedOrder.Count == 0)
        {
            return;
        }

        var order = persistedOrder
            .Select((filterKey, index) => new { filterKey, index })
            .GroupBy(item => item.filterKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.OrdinalIgnoreCase);

        var orderedItems = dashboard
            .Select((metric, index) => new { metric, index })
            .OrderBy(item => order.TryGetValue(item.metric.FilterKey, out var persistedIndex) ? persistedIndex : int.MaxValue)
            .ThenBy(item => item.index)
            .Select(item => item.metric)
            .ToList();

        dashboard.Clear();
        foreach (var metric in orderedItems)
        {
            dashboard.Add(metric);
        }
    }

    private Task SaveMetricPreferencesAsync()
    {
        if (!hasLoadedMetricPreferences)
        {
            return Task.CompletedTask;
        }

        return metricPreferences.SaveAsync(CreateMetricPreferencesSnapshot());
    }

    private DashboardMetricPreferencesDto CreateMetricPreferencesSnapshot()
    {
        return new DashboardMetricPreferencesDto
        {
            GeneralOrder = MetricsDashboard.Select(metric => metric.FilterKey).ToList(),
            HealthOrder = HealthDashboard.Select(metric => metric.FilterKey).ToList(),
            RemovedGeneralRootFilterKeys = removedGeneralRootFilterKeys.ToList(),
            RemovedHealthRootFilterKeys = removedHealthRootFilterKeys.ToList(),
            Combinations = metricCombinations
                .GroupBy(GetCombinationFilterKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Select(combination => new DashboardMetricCombinationPreferenceDto
                {
                    IsHealth = combination.IsHealth,
                    FirstFilterKey = combination.FirstFilterKey,
                    SecondFilterKey = combination.SecondFilterKey,
                    SexModifier = combination.SexModifier,
                    MinimumAgeModifier = combination.MinimumAgeModifier,
                    MaximumAgeModifier = combination.MaximumAgeModifier
                })
                .ToList()
        };
    }

    private static string GetCombinationFilterKey(MetricCombination combination)
    {
        return DashboardFilterKeys.CreateModified(
            DashboardFilterKeys.CreateCombination(combination.FirstFilterKey, combination.SecondFilterKey),
            combination.SexModifier,
            combination.MinimumAgeModifier,
            combination.MaximumAgeModifier);
    }

    private sealed record MetricModifierSelection(string? Sex = null, int? MinimumAge = null, int? MaximumAge = null);

    private sealed record MetricCombination(
        bool IsHealth,
        string FirstFilterKey,
        string SecondFilterKey,
        string? SexModifier,
        int? MinimumAgeModifier,
        int? MaximumAgeModifier);
}
