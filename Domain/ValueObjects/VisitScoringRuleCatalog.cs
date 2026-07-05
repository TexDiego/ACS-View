using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class VisitScoringRuleCatalog
{
    public const string CurrentRuleVersion = "APS-2026.1";

    public static IReadOnlyList<VisitCareLineRule> ActiveRules { get; } =
    [
        new(VisitCareLineType.Child, 2, null, null, 20, "Crianca ate 2 anos: 1 visita ate 30 dias e 2 visita ate 6 meses", "ChildTwoStep", CurrentRuleVersion, true),
        new(VisitCareLineType.Pregnancy, 3, null, null, 9, "Gestante: 3 visitas durante o pre-natal quando houver dados suficientes", "PregnancyPrenatal", CurrentRuleVersion, true),
        new(VisitCareLineType.Postpartum, 1, null, null, 9, "Puerpera: 1 visita puerperal quando houver dados suficientes", "Postpartum", CurrentRuleVersion, true),
        new(VisitCareLineType.Diabetes, 2, 12, 30, 20, "Diabetes: 2 visitas realizadas nos ultimos 12 meses com intervalo minimo de 30 dias", "Rolling12Months", CurrentRuleVersion, true),
        new(VisitCareLineType.Hypertension, 2, 12, 30, 25, "Hipertensao: 2 visitas realizadas nos ultimos 12 meses com intervalo minimo de 30 dias", "Rolling12Months", CurrentRuleVersion, true),
        new(VisitCareLineType.Elderly, 2, 12, 30, 25, "Idoso: 2 visitas realizadas nos ultimos 12 meses com intervalo minimo de 30 dias", "Rolling12Months", CurrentRuleVersion, true)
    ];

    public static VisitCareLineRule? GetRule(VisitCareLineType type)
    {
        return ActiveRules.FirstOrDefault(rule => rule.CareLineType == type && rule.IsActive);
    }
}

public sealed record VisitCareLineRule(
    VisitCareLineType CareLineType,
    int RequiredVisits,
    int? PeriodInMonths,
    int? MinimumDaysBetweenVisits,
    int Points,
    string Description,
    string DeadlineRule,
    string RuleVersion,
    bool IsActive);
