namespace ACS_View.Domain.ValueObjects;

public static class DashboardMetricCombinationRules
{
    private const string Female = "Feminino";

    private static readonly HashSet<string> NonCombinableFilterKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        DashboardFilterKeys.All,
        DashboardFilterKeys.Residences,
        DashboardFilterKeys.Families,
        DashboardFilterKeys.EmptyResidences,
        DashboardFilterKeys.Inactive
    };

    private static readonly HashSet<string> PatientGeneralFilterKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        DashboardFilterKeys.NoHouse,
        DashboardFilterKeys.NoFamily,
        DashboardFilterKeys.BolsaFamilia,
        DashboardFilterKeys.Elderly,
        DashboardFilterKeys.ChildrenUnder6,
        DashboardFilterKeys.Women25To64
    };

    public static bool IsCombinableRootMetric(string? filterKey)
    {
        var baseFilterKey = DashboardFilterKeys.GetBaseFilterKey(filterKey);

        if (string.IsNullOrWhiteSpace(baseFilterKey) ||
            DashboardFilterKeys.IsCombination(baseFilterKey) ||
            NonCombinableFilterKeys.Contains(baseFilterKey))
        {
            return false;
        }

        return IsHealthMetric(baseFilterKey) || PatientGeneralFilterKeys.Contains(baseFilterKey);
    }

    public static bool IsHealthMetric(string? filterKey)
    {
        var baseFilterKey = DashboardFilterKeys.GetBaseFilterKey(filterKey);
        return string.Equals(baseFilterKey, DashboardFilterKeys.ChildrenOverdueVaccines, StringComparison.OrdinalIgnoreCase) ||
               IsPregnancyMetric(baseFilterKey) ||
               baseFilterKey.StartsWith(DashboardFilterKeys.ConditionPrefix, StringComparison.OrdinalIgnoreCase) ||
               baseFilterKey.StartsWith(DashboardFilterKeys.CidPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanCombine(
        string? firstFilterKey,
        string? secondFilterKey,
        bool targetIsHealth,
        string? sexModifier = null,
        int? minimumAgeModifier = null,
        int? maximumAgeModifier = null)
    {
        return CanCombine(
            [firstFilterKey ?? string.Empty, secondFilterKey ?? string.Empty],
            targetIsHealth,
            sexModifier,
            minimumAgeModifier,
            maximumAgeModifier);
    }

    public static bool CanCombine(
        IReadOnlyList<string> filterKeys,
        bool targetIsHealth,
        string? sexModifier = null,
        int? minimumAgeModifier = null,
        int? maximumAgeModifier = null)
    {
        return GetValidationError(
            filterKeys,
            targetIsHealth,
            sexModifier,
            minimumAgeModifier,
            maximumAgeModifier) is null;
    }

    public static string? GetValidationError(
        string? firstFilterKey,
        string? secondFilterKey,
        bool targetIsHealth,
        string? sexModifier = null,
        int? minimumAgeModifier = null,
        int? maximumAgeModifier = null)
    {
        return GetValidationError(
            [firstFilterKey ?? string.Empty, secondFilterKey ?? string.Empty],
            targetIsHealth,
            sexModifier,
            minimumAgeModifier,
            maximumAgeModifier);
    }

    public static string? GetValidationError(
        IReadOnlyList<string> filterKeys,
        bool targetIsHealth,
        string? sexModifier = null,
        int? minimumAgeModifier = null,
        int? maximumAgeModifier = null)
    {
        var baseFilterKeys = filterKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(DashboardFilterKeys.GetBaseFilterKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (baseFilterKeys.Count == 0)
        {
            return "Selecione ao menos uma metrica.";
        }

        if (baseFilterKeys.Count > 3)
        {
            return "Selecione no maximo tres metricas.";
        }

        var hasModifier =
            !string.IsNullOrWhiteSpace(sexModifier) ||
            minimumAgeModifier.HasValue ||
            maximumAgeModifier.HasValue;

        if (baseFilterKeys.Count == 1 && !hasModifier)
        {
            return "Adicione ao menos um modificador para criar uma metrica com um unico indicador.";
        }

        if (baseFilterKeys.Any(key => !IsCombinableRootMetric(key)))
        {
            return "Essa metrica nao pode fazer parte de unioes.";
        }

        var hasHealthMetric = baseFilterKeys.Any(IsHealthMetric);
        if (targetIsHealth && !hasHealthMetric)
        {
            return "Unioes na aba de saude precisam ter ao menos uma metrica de saude.";
        }

        if (!targetIsHealth && hasHealthMetric)
        {
            return "Unioes com metricas de saude devem ficar na aba de saude.";
        }

        var constraints = new MetricConstraints();
        foreach (var key in baseFilterKeys)
        {
            if (!TryApplyMetricConstraints(key, ref constraints))
            {
                return "Essa uniao nao e compativel com a logica das metricas selecionadas.";
            }
        }

        if (!TryApplyModifierConstraints(sexModifier, minimumAgeModifier, maximumAgeModifier, ref constraints))
        {
            return "Essa uniao nao e compativel com a logica das metricas selecionadas.";
        }

        return null;
    }

    private static bool TryApplyMetricConstraints(string filterKey, ref MetricConstraints constraints)
    {
        switch (filterKey)
        {
            case DashboardFilterKeys.Elderly:
                return constraints.ApplyAge(minimumAge: 60, maximumAge: null);
            case DashboardFilterKeys.ChildrenUnder6:
                return constraints.ApplyAge(minimumAge: null, maximumAge: 5);
            case DashboardFilterKeys.ChildrenOverdueVaccines:
                return constraints.ApplyAge(minimumAge: null, maximumAge: 10);
            case DashboardFilterKeys.Women25To64:
                return constraints.ApplySex(Female) &&
                       constraints.ApplyAge(minimumAge: 25, maximumAge: 64);
            case DashboardFilterKeys.Pregnant:
            case DashboardFilterKeys.PregnancyDueDateSoon:
            case DashboardFilterKeys.PregnancyThirdTrimester:
            case DashboardFilterKeys.PregnancyMissingDates:
            case DashboardFilterKeys.PregnancyHighRisk:
            case DashboardFilterKeys.PregnancySuggestedAttention:
            case DashboardFilterKeys.PregnancySuggestedHighRisk:
            case DashboardFilterKeys.PregnancyRiskNotInformed:
            case DashboardFilterKeys.ActivePuerperal:
                return constraints.ApplySex(Female);
            default:
                return true;
        }
    }

    private static bool IsPregnancyMetric(string filterKey)
    {
        return string.Equals(filterKey, DashboardFilterKeys.Pregnant, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(filterKey, DashboardFilterKeys.PregnancyDueDateSoon, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(filterKey, DashboardFilterKeys.PregnancyThirdTrimester, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(filterKey, DashboardFilterKeys.PregnancyMissingDates, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(filterKey, DashboardFilterKeys.PregnancyHighRisk, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(filterKey, DashboardFilterKeys.PregnancySuggestedAttention, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(filterKey, DashboardFilterKeys.PregnancySuggestedHighRisk, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(filterKey, DashboardFilterKeys.PregnancyRiskNotInformed, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(filterKey, DashboardFilterKeys.ActivePuerperal, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryApplyModifierConstraints(
        string? sexModifier,
        int? minimumAgeModifier,
        int? maximumAgeModifier,
        ref MetricConstraints constraints)
    {
        if (!constraints.ApplySex(sexModifier))
        {
            return false;
        }

        return constraints.ApplyAge(minimumAgeModifier, maximumAgeModifier);
    }

    private struct MetricConstraints
    {
        private string? sex;
        private int? minimumAge;
        private int? maximumAge;

        public bool ApplySex(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            var normalized = value.Trim();
            if (string.IsNullOrWhiteSpace(sex))
            {
                sex = normalized;
                return true;
            }

            return string.Equals(sex, normalized, StringComparison.OrdinalIgnoreCase);
        }

        public bool ApplyAge(int? minimumAge, int? maximumAge)
        {
            if (minimumAge.HasValue)
            {
                this.minimumAge = this.minimumAge.HasValue
                    ? Math.Max(this.minimumAge.Value, minimumAge.Value)
                    : minimumAge.Value;
            }

            if (maximumAge.HasValue)
            {
                this.maximumAge = this.maximumAge.HasValue
                    ? Math.Min(this.maximumAge.Value, maximumAge.Value)
                    : maximumAge.Value;
            }

            return !this.minimumAge.HasValue ||
                   !this.maximumAge.HasValue ||
                   this.minimumAge.Value <= this.maximumAge.Value;
        }
    }
}
