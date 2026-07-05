namespace ACS_View.Domain.ValueObjects;

public static class DashboardFilterKeys
{
    public const string All = "ALL";
    public const string Residences = "RESIDENCES";
    public const string Families = "FAMILIES";
    public const string EmptyResidences = "EMPTY_RESIDENCES";
    public const string NoHouse = "NO_HOUSE";
    public const string NoFamily = "NO_FAMILY";
    public const string BolsaFamilia = "BOLSA_FAMILIA";
    public const string Elderly = "ELDERLY";
    public const string ChildrenUnder6 = "CHILDREN_UNDER_6";
    public const string ChildrenOverdueVaccines = "CHILDREN_OVERDUE_VACCINES";
    public const string Women25To64 = "WOMEN_25_64";
    public const string Inactive = "INACTIVE";

    public const string ConditionPrefix = "COND:";
    public const string CidPrefix = "CID:";
    public const string CombinationPrefix = "COMBO:";
    private const string CombinationDelimiter = "&&";
    private const string ModifierDelimiter = "||";
    private const string SexModifierPrefix = "SEX:";
    private const string MinAgeModifierPrefix = "MINAGE:";
    private const string MaxAgeModifierPrefix = "MAXAGE:";

    public static string CreateCombination(params string[] filterKeys)
    {
        return CreateCombination((IEnumerable<string>)filterKeys);
    }

    public static string CreateCombination(IEnumerable<string> filterKeys)
    {
        var parts = filterKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return $"{CombinationPrefix}{string.Join(CombinationDelimiter, parts)}";
    }

    public static string CreateModified(string baseFilterKey, string? sex, int? minimumAge, int? maximumAge)
    {
        var parts = new List<string> { GetBaseFilterKey(baseFilterKey) };

        if (!string.IsNullOrWhiteSpace(sex))
        {
            parts.Add($"{SexModifierPrefix}{sex.Trim()}");
        }

        if (minimumAge.HasValue)
        {
            parts.Add($"{MinAgeModifierPrefix}{minimumAge.Value}");
        }

        if (maximumAge.HasValue)
        {
            parts.Add($"{MaxAgeModifierPrefix}{maximumAge.Value}");
        }

        return string.Join(ModifierDelimiter, parts);
    }

    public static string GetBaseFilterKey(string? filterKey)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
        {
            return All;
        }

        return filterKey.Split(ModifierDelimiter, StringSplitOptions.None)[0];
    }

    public static DashboardFilterModifiers GetModifiers(string? filterKey)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
        {
            return new DashboardFilterModifiers();
        }

        string? sex = null;
        int? minimumAge = null;
        int? maximumAge = null;

        foreach (var part in filterKey.Split(ModifierDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1))
        {
            if (part.StartsWith(SexModifierPrefix, StringComparison.OrdinalIgnoreCase))
            {
                sex = part[SexModifierPrefix.Length..];
            }
            else if (part.StartsWith(MinAgeModifierPrefix, StringComparison.OrdinalIgnoreCase) &&
                     int.TryParse(part[MinAgeModifierPrefix.Length..], out var minAge))
            {
                minimumAge = minAge;
            }
            else if (part.StartsWith(MaxAgeModifierPrefix, StringComparison.OrdinalIgnoreCase) &&
                     int.TryParse(part[MaxAgeModifierPrefix.Length..], out var maxAge))
            {
                maximumAge = maxAge;
            }
        }

        return new DashboardFilterModifiers(sex, minimumAge, maximumAge);
    }

    public static bool IsCombination(string? filterKey)
    {
        return GetBaseFilterKey(filterKey).StartsWith(CombinationPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> GetCombinationParts(string? filterKey)
    {
        if (!IsCombination(filterKey))
        {
            return [];
        }

        var baseFilterKey = GetBaseFilterKey(filterKey);
        return baseFilterKey[CombinationPrefix.Length..]
            .Split(CombinationDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(key => !IsCombination(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }
}

public sealed record DashboardFilterModifiers(string? Sex = null, int? MinimumAge = null, int? MaximumAge = null);
