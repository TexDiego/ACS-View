namespace ACS_View.Application.DTOs;

public sealed class DashboardMetricPreferencesDto
{
    public List<string> GeneralOrder { get; set; } = [];
    public List<string> HealthOrder { get; set; } = [];
    public List<string> RemovedGeneralRootFilterKeys { get; set; } = [];
    public List<string> RemovedHealthRootFilterKeys { get; set; } = [];
    public List<DashboardMetricCombinationPreferenceDto> Combinations { get; set; } = [];
}

public sealed class DashboardMetricCombinationPreferenceDto
{
    public bool IsHealth { get; set; }
    public List<string> FilterKeys { get; set; } = [];
    public string FirstFilterKey { get; set; } = string.Empty;
    public string SecondFilterKey { get; set; } = string.Empty;
    public string? SexModifier { get; set; }
    public int? MinimumAgeModifier { get; set; }
    public int? MaximumAgeModifier { get; set; }

    public IReadOnlyList<string> GetSelectedFilterKeys()
    {
        var keys = FilterKeys.Count > 0
            ? FilterKeys
            : [FirstFilterKey, SecondFilterKey];

        return keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
