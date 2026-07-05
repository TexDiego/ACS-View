namespace ACS_View.Application.DTOs;

public sealed class DashboardMetricCreateRequestDto
{
    public List<string> FilterKeys { get; init; } = [];
    public string FirstFilterKey { get; init; } = string.Empty;
    public string SecondFilterKey { get; init; } = string.Empty;
    public string? SexModifier { get; init; }
    public int? MinimumAgeModifier { get; init; }
    public int? MaximumAgeModifier { get; init; }

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
