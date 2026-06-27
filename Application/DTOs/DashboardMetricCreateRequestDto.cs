namespace ACS_View.Application.DTOs;

public sealed class DashboardMetricCreateRequestDto
{
    public string FirstFilterKey { get; init; } = string.Empty;
    public string SecondFilterKey { get; init; } = string.Empty;
    public string? SexModifier { get; init; }
    public int? MinimumAgeModifier { get; init; }
    public int? MaximumAgeModifier { get; init; }
}
