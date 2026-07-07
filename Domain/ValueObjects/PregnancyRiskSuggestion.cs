using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public sealed class PregnancyRiskSuggestion
{
    public PregnancyRisk Risk { get; set; } = PregnancyRisk.Usual;
    public int Score { get; set; }
    public List<string> Reasons { get; set; } = [];

    public string ReasonsText => Reasons.Count == 0
        ? string.Empty
        : string.Join(" · ", Reasons);
}
