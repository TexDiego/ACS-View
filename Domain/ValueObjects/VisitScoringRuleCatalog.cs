using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class VisitScoringRuleCatalog
{
    public const string CurrentRuleVersion = "APS-2026.1";

    public static IReadOnlyList<VisitCareLineRule> ActiveRules { get; } =
    [
        new(VisitCareLineType.Child, 2, null, null, 20, "Criança até 2 anos: 1 visita até 30 dias e 2 visitas até 6 meses", "ChildTwoStep", CurrentRuleVersion, true),
        new(VisitCareLineType.Pregnancy, 3, null, null, 9, "Gestante: 3 visitas durante o pré-natal quando houver dados suficientes", "PregnancyPrenatal", CurrentRuleVersion, true),
        new(VisitCareLineType.Postpartum, 1, null, null, 9, "Puérpera: 1 visita puerperal quando houver dados suficientes", "Postpartum", CurrentRuleVersion, true),
        new(VisitCareLineType.Diabetes, 2, 1, null, 20, "Diabetes: 2 visitas no mês, em dias diferentes", "CurrentMonth", CurrentRuleVersion, true),
        new(VisitCareLineType.Hypertension, 2, 1, null, 25, "Hipertensão: 2 visitas no mês, em dias diferentes", "CurrentMonth", CurrentRuleVersion, true),
        new(VisitCareLineType.Elderly, 2, 1, null, 25, "Idoso: 2 visitas no mês, em dias diferentes", "CurrentMonth", CurrentRuleVersion, true),
        new(VisitCareLineType.BolsaFamilia, 2, 1, null, 18, "Bolsa Família: 2 visitas no mês, em dias diferentes", "CurrentMonth", CurrentRuleVersion, true),
        new(VisitCareLineType.Bpc, 2, 1, null, 18, "BPC: 2 visitas no mês, em dias diferentes", "CurrentMonth", CurrentRuleVersion, true),
        new(VisitCareLineType.NoVulnerability, 1, 1, null, 10, "Sem critérios de vulnerabilidade: 1 visita no mês", "CurrentMonth", CurrentRuleVersion, true)
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
