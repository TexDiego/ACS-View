namespace ACS_View.Domain.ValueObjects;

public static class DashboardFilterKeys
{
    public const string All = "ALL";
    public const string Residences = "RESIDENCES";
    public const string NoHouse = "NO_HOUSE";
    public const string NoFamily = "NO_FAMILY";
    public const string BolsaFamilia = "BOLSA_FAMILIA";
    public const string Elderly = "ELDERLY";
    public const string ChildrenUnder6 = "CHILDREN_UNDER_6";

    public const string ConditionPrefix = "COND:";
    public const string CidPrefix = "CID:";
    public const string CombinationPrefix = "COMBO:";
    private const string CombinationDelimiter = "&&";

    public static string CreateCombination(string firstFilterKey, string secondFilterKey)
    {
        var parts = new[] { firstFilterKey, secondFilterKey }
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return $"{CombinationPrefix}{string.Join(CombinationDelimiter, parts)}";
    }

    public static bool IsCombination(string? filterKey)
    {
        return filterKey?.StartsWith(CombinationPrefix, StringComparison.OrdinalIgnoreCase) == true;
    }

    public static IReadOnlyList<string> GetCombinationParts(string? filterKey)
    {
        if (!IsCombination(filterKey))
        {
            return [];
        }

        return filterKey![CombinationPrefix.Length..]
            .Split(CombinationDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(key => !IsCombination(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();
    }
}
