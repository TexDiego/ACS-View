using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public sealed record VisitMilestoneResult(string Name, DateTime Deadline, bool Completed);

public enum VisitFollowUpStatus { NoSpecificRecommendation, UpToDate, Recommended, DueSoon, Overdue, InsufficientData }

public sealed record VisitRuleEvaluation(VisitCareLineType CareLine, int Required, int Completed,
    DateTime? RecommendedDate, DateTime? Deadline, VisitFollowUpStatus Status, string Progress,
    string Source, bool IncompleteHistory)
{
    public IReadOnlyList<VisitMilestoneResult> Milestones { get; init; } = [];
    public string Label => CareLine switch
    {
        VisitCareLineType.Child => "Desenvolvimento infantil",
        VisitCareLineType.Pregnancy => "Gestação",
        VisitCareLineType.Postpartum => "Puerpério (rotina local)",
        VisitCareLineType.NoVulnerability => "Rotina local",
        VisitCareLineType.BolsaFamilia => "Bolsa Família (rotina local)",
        VisitCareLineType.Bpc => "BPC (rotina local)",
        VisitCareLineType.Hypertension => "Hipertensão",
        VisitCareLineType.Elderly => "Pessoa idosa",
        _ => "Diabetes"
    };
    public string RuleVersion => VisitScoringRuleCatalog.CurrentRuleVersion;
    public int Missing => Math.Max(0, Required - Completed);
    public int Urgency => Status switch { VisitFollowUpStatus.Overdue => 5, VisitFollowUpStatus.DueSoon => 4,
        VisitFollowUpStatus.Recommended => 3, VisitFollowUpStatus.InsufficientData => 2, _ => 0 };
    public string StatusText => Status switch { VisitFollowUpStatus.UpToDate => "Acompanhamento em dia",
        VisitFollowUpStatus.Recommended => "Visita recomendada", VisitFollowUpStatus.DueSoon => "Prazo próximo",
        VisitFollowUpStatus.Overdue => "Prazo vencido", VisitFollowUpStatus.InsufficientData => "Dados insuficientes",
        _ => "Sem recomendação específica" };
}

// Pure evaluator. Historical care-line snapshots are required; missing snapshots are never backfilled.
public static class VisitFollowUpCalculator
{
    public static VisitRuleEvaluation Evaluate(VisitCareLineType line, DateTime? birthDate,
        PatientPregnancy? pregnancy, IEnumerable<DateTime> completedEvidence, bool incompleteHistory, DateTime referenceDate)
    {
        var today = referenceDate.Date;
        var rule = VisitScoringRuleCatalog.GetRule(line)!;
        var dates = completedEvidence.Select(d => d.Date).Where(d => d <= today).Distinct().Order().ToList();
        DateTime? start = null, deadline = null, recommended = today;
        int count = 0;
        string progress;
        bool insufficient = false;
        if (rule.Period == VisitRulePeriod.RollingMonths)
        {
            // Inclusive calendar-date window [reference.AddMonths(-12), reference].
            start = today.AddMonths(-rule.PeriodInMonths!.Value);
            dates = dates.Where(d => d >= start).ToList();
            var selected = SelectSpaced(dates, rule.MinimumDaysBetweenVisits!.Value);
            count = selected.Count;
            if (count >= rule.RequiredVisits)
            {
                // Re-evaluate at every expiry: redundant visits may keep the indicator satisfied.
                var expiryDates = dates.Select(ExpiryDate).Distinct().Order();
                var expiry = expiryDates.First(day => SelectSpaced(dates.Where(d => d >= day.AddMonths(-12)), 30).Count < rule.RequiredVisits);
                deadline = expiry.AddDays(-1);
                recommended = deadline;
            }
            else if (dates.Count > 0) recommended = dates[0].AddDays(30) > today ? dates[0].AddDays(30) : today;
            progress = $"{Math.Min(count, rule.RequiredVisits)} de {rule.RequiredVisits} visitas válidas nos últimos 12 meses.";
        }
        else if (rule.Period == VisitRulePeriod.ChildMilestones)
        {
            if (birthDate is null || birthDate > today || birthDate == default(DateTime))
                return Result(0, null, null, VisitFollowUpStatus.InsufficientData, "Data de nascimento ausente ou inconsistente.");
            var birth = birthDate.Value.Date;
            if (today >= birth.AddYears(2))
                return Result(0, null, null, VisitFollowUpStatus.NoSpecificRecommendation, "Fora da faixa etária do indicador infantil.");
            var first = dates.FirstOrDefault(d => d >= birth && d <= birth.AddDays(30));
            bool firstDone = first != default;
            bool secondDone = dates.Count(d => d >= birth && d <= birth.AddMonths(6)) >= 2;
            count = (firstDone ? 1 : 0) + (secondDone ? 1 : 0);
            deadline = firstDone ? birth.AddMonths(6) : birth.AddDays(30);
            progress = $"Primeira visita: {(firstDone ? "realizada" : "pendente")}. Segunda: {(secondDone ? "realizada" : "pendente")}.";
        }
        else if (rule.Period is VisitRulePeriod.PregnancyEpisode or VisitRulePeriod.PostpartumEpisode)
        {
            if (line == VisitCareLineType.Postpartum)
            {
                start = pregnancy?.Status == PregnancyStatus.Ended && pregnancy.EndType == PregnancyEndType.Birth ? pregnancy.EndedAt?.Date : null;
                deadline = start?.AddDays(PregnancyCalculator.DefaultPuerperiumDays);
            }
            else
            {
                start = pregnancy?.LastMenstrualPeriod?.Date ?? pregnancy?.ExpectedBirthDate?.Date.AddDays(-280);
                // DPP is a planning estimate, never a normative prenatal deadline.
                var end = pregnancy?.ExpectedBirthDate?.Date ?? start?.AddDays(280);
                if (end.HasValue && end > today) recommended = today.AddDays(Math.Max(0,(end.Value-today).Days / (rule.RequiredVisits + 1)));
                if (pregnancy?.Status != PregnancyStatus.Active) insufficient = true;
            }
            insufficient |= start is null || start > today;
            count = start.HasValue ? dates.Count(d => d >= start && (deadline is null || d <= deadline)) : 0;
            if (line == VisitCareLineType.Pregnancy && count < rule.RequiredVisits)
            {
                var end = pregnancy?.ExpectedBirthDate?.Date ?? start?.AddDays(280);
                recommended = start.HasValue && end.HasValue ? today.AddDays(Math.Max(0,(end.Value-today).Days / (rule.RequiredVisits-count+1))) : null;
            }
            progress = insufficient ? "Episódio sem dados suficientes para contabilizar visitas." : $"{Math.Min(count,rule.RequiredVisits)} de {rule.RequiredVisits} visitas no episódio.";
        }
        else
        {
            start = new DateTime(today.Year,today.Month,1);
            deadline = start.Value.AddMonths(1).AddDays(-1);
            count = dates.Count(d => d >= start);
            progress = $"Rotina operacional: {count} de {rule.RequiredVisits} visitas no mês.";
        }
        count = Math.Min(count, rule.RequiredVisits);
        var status = insufficient ? VisitFollowUpStatus.InsufficientData : count >= rule.RequiredVisits ? VisitFollowUpStatus.UpToDate
            : deadline < today ? VisitFollowUpStatus.Overdue : deadline <= today.AddDays(7) ? VisitFollowUpStatus.DueSoon : VisitFollowUpStatus.Recommended;
        // Satisfied rolling indicators approaching expiry still need planning priority.
        if (count >= rule.RequiredVisits && rule.Period == VisitRulePeriod.RollingMonths && deadline <= today.AddDays(7)) status = VisitFollowUpStatus.DueSoon;
        if (count >= rule.RequiredVisits && rule.Period != VisitRulePeriod.RollingMonths) recommended = null;
        // A second visit on an already counted day cannot advance these indicators.
        if (count < rule.RequiredVisits && recommended.HasValue)
            while (dates.Contains(recommended.Value)) recommended = recommended.Value.AddDays(1);
        return Result(count, insufficient ? null : recommended, deadline, status, progress);

        VisitRuleEvaluation Result(int completed, DateTime? next, DateTime? due, VisitFollowUpStatus state, string text) =>
            new(line,rule.RequiredVisits,completed,next,due,state,text,rule.Source,incompleteHistory)
            {
                Milestones = rule.Period == VisitRulePeriod.ChildMilestones && birthDate.HasValue && birthDate <= today && birthDate > DateTime.MinValue
                    ? [new("Primeira visita", birthDate.Value.AddDays(30), dates.Any(d=>d>=birthDate && d<=birthDate.Value.AddDays(30))),
                       new("Segunda visita", birthDate.Value.AddMonths(6), dates.Count(d=>d>=birthDate && d<=birthDate.Value.AddMonths(6))>=2)] : []
            };
    }

    public static List<DateTime> SelectSpaced(IEnumerable<DateTime> dates, int minimumDays)
    {
        var selected = new List<DateTime>();
        foreach (var day in dates.Select(d => d.Date).Distinct().Order())
            if (selected.Count == 0 || (day-selected[^1]).Days >= minimumDays) selected.Add(day);
        return selected;
    }

    private static DateTime ExpiryDate(DateTime visit)
    {
        var expiry = visit.AddMonths(12);
        while (expiry.AddMonths(-12) <= visit) expiry = expiry.AddDays(1);
        while (expiry.AddDays(-1).AddMonths(-12) > visit) expiry = expiry.AddDays(-1);
        return expiry;
    }
}
