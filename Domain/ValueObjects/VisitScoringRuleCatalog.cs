using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class VisitScoringRuleCatalog
{
    public const string PreviousRuleVersion = "APS-2026.1";
    public const string CurrentRuleVersion = "APS-2026.2";

    public static IReadOnlyList<VisitCareLineRule> ActiveRules { get; } =
    [
        new(VisitCareLineType.Child, 2, null, null, 20, "Criança até 2 anos: 1 visita até 30 dias e 2 visitas até 6 meses", "ChildTwoStep", CurrentRuleVersion, true),
        new(VisitCareLineType.Pregnancy, 3, null, null, 9, "Gestante: 3 visitas durante o pré-natal quando houver dados suficientes", "PregnancyPrenatal", CurrentRuleVersion, true),
        new(VisitCareLineType.Postpartum, 1, null, null, 9, "Puérpera: 1 visita puerperal quando houver dados suficientes", "Postpartum", CurrentRuleVersion, true),
        new(VisitCareLineType.Diabetes, 2, 12, 30, 20, "Diabetes: 2 visitas nos últimos 12 meses, intervalo mínimo de 30 dias", "RollingYear", CurrentRuleVersion, true),
        new(VisitCareLineType.Hypertension, 2, 12, 30, 25, "Hipertensão: 2 visitas nos últimos 12 meses, intervalo mínimo de 30 dias", "RollingYear", CurrentRuleVersion, true),
        new(VisitCareLineType.Elderly, 2, 12, 30, 25, "Idoso: 2 visitas nos últimos 12 meses, intervalo mínimo de 30 dias", "RollingYear", CurrentRuleVersion, true),
        new(VisitCareLineType.BolsaFamilia, 2, 1, null, 18, "Bolsa Família: 2 visitas no mês, em dias diferentes", "CurrentMonth", CurrentRuleVersion, true),
        new(VisitCareLineType.Bpc, 2, 1, null, 18, "BPC: 2 visitas no mês, em dias diferentes", "CurrentMonth", CurrentRuleVersion, true),
        new(VisitCareLineType.NoVulnerability, 1, 1, null, 5, "Sem critérios de vulnerabilidade: 1 visita no mês", "CurrentMonth", CurrentRuleVersion, true)
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
    bool IsActive)
{
    public VisitRulePeriod Period => CareLineType switch
    {
        VisitCareLineType.Child => VisitRulePeriod.ChildMilestones,
        VisitCareLineType.Pregnancy => VisitRulePeriod.PregnancyEpisode,
        VisitCareLineType.Postpartum => VisitRulePeriod.PostpartumEpisode,
        VisitCareLineType.Diabetes or VisitCareLineType.Hypertension or VisitCareLineType.Elderly => VisitRulePeriod.RollingMonths,
        _ => VisitRulePeriod.CalendarMonth
    };
    public string Eligibility => CareLineType switch
    {
        VisitCareLineType.Child => "Nascimento válido; idade inferior a dois anos",
        VisitCareLineType.Elderly => "Nascimento válido; idade igual ou superior a 60 anos",
        VisitCareLineType.Pregnancy => "Episódio ativo; condição legada sem episódio sinaliza dados insuficientes",
        VisitCareLineType.Postpartum => "Parto registrado; até 42 dias após o parto",
        VisitCareLineType.BolsaFamilia => "Vínculo em PatientBolsaFamilia",
        VisitCareLineType.Bpc => "Sem fonte cadastral disponível; não inferir elegibilidade",
        VisitCareLineType.NoVulnerability => "Ausência de outras linhas identificadas",
        _ => "Condição cadastrada em PatientConditions"
    };
    public bool IsLocal => Period is VisitRulePeriod.CalendarMonth or VisitRulePeriod.PostpartumEpisode;
    public string Source => IsLocal ? "Regra operacional local ACS View" : CareLineType switch
    {
        VisitCareLineType.Child => "Anexo 14, C2, página 11",
        VisitCareLineType.Pregnancy => "Anexo 14, C3, página 14",
        VisitCareLineType.Diabetes => "Anexo 14, C4, página 16",
        VisitCareLineType.Hypertension => "Anexo 14, C5, página 18",
        _ => "Anexo 14, C6, página 20"
    };
}
public enum VisitRulePeriod { CalendarMonth, RollingMonths, ChildMilestones, PregnancyEpisode, PostpartumEpisode }
