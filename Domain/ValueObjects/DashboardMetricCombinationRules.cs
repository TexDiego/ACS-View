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
        return baseFilterKey.StartsWith(DashboardFilterKeys.ConditionPrefix, StringComparison.OrdinalIgnoreCase) ||
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
        return GetValidationError(
            firstFilterKey,
            secondFilterKey,
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
        var firstBaseFilterKey = DashboardFilterKeys.GetBaseFilterKey(firstFilterKey);
        var secondBaseFilterKey = DashboardFilterKeys.GetBaseFilterKey(secondFilterKey);

        if (string.Equals(firstBaseFilterKey, secondBaseFilterKey, StringComparison.OrdinalIgnoreCase))
        {
            return "Selecione duas métricas diferentes.";
        }

        if (!IsCombinableRootMetric(firstBaseFilterKey) || !IsCombinableRootMetric(secondBaseFilterKey))
        {
            return "Essa métrica não pode fazer parte de uniões.";
        }

        var firstIsHealth = IsHealthMetric(firstBaseFilterKey);
        var secondIsHealth = IsHealthMetric(secondBaseFilterKey);

        if (targetIsHealth && !firstIsHealth && !secondIsHealth)
        {
            return "Uniões na aba de saúde precisam ter ao menos uma métrica de saúde.";
        }

        if (!targetIsHealth && (firstIsHealth || secondIsHealth))
        {
            return "Uniões com métricas de saúde devem ficar na aba de saúde.";
        }

        var constraints = new MetricConstraints();
        if (!TryApplyMetricConstraints(firstBaseFilterKey, ref constraints) ||
            !TryApplyMetricConstraints(secondBaseFilterKey, ref constraints) ||
            !TryApplyModifierConstraints(sexModifier, minimumAgeModifier, maximumAgeModifier, ref constraints))
        {
            return "Essa união não é compatível com a lógica das métricas selecionadas.";
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
            case DashboardFilterKeys.Women25To64:
                return constraints.ApplySex(Female) &&
                       constraints.ApplyAge(minimumAge: 25, maximumAge: 64);
            default:
                return true;
        }
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
