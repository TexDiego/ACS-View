using ACS_View.Domain.ValueObjects;
namespace ACS_View.Application.DTOs;

public sealed class PatientVisitSummaryDto
{
    public string DataQualityNote { get; init; } = string.Empty;
    public int PatientId { get; init; }
    public DateTime ReferenceMonth { get; init; }
    public int MonthlyCompletedVisits { get; init; }
    public DateTime? LastCompletedVisit { get; init; }
    public DateTime? LastAttempt { get; init; }
    public string LastAttemptStatus { get; init; } = string.Empty;
    public IReadOnlyList<VisitRuleEvaluation> Evaluations { get; init; } = [];
    public VisitRuleEvaluation? Primary => Evaluations.OrderByDescending(e => e.Urgency).ThenBy(e => e.Deadline ?? DateTime.MaxValue).FirstOrDefault();
    public string DisplayText => string.Join("\n", new[] {
        LastCompletedVisit.HasValue ? $"Última visita realizada: {LastCompletedVisit:dd/MM/yyyy}" : LastAttempt.HasValue ? "Nenhuma visita concluída registrada." : "Nenhuma visita registrada.",
        LastAttempt.HasValue && LastAttempt > LastCompletedVisit.GetValueOrDefault() && LastAttemptStatus != "Completed" ? $"Última tentativa: {LastAttempt:dd/MM/yyyy} — {(LastAttemptStatus == "Absent" ? "Ausente" : "Recusada")}" : null,
        ReferenceMonth != default ? $"Visitas realizadas em {ReferenceMonth:MM/yyyy}: {MonthlyCompletedVisits}" : null,
        DataQualityNote,
        Primary?.StatusText ?? "Sem recomendação específica",
        Primary?.RecommendedDate is DateTime next ? $"Próxima visita sugerida (planejamento): {next:dd/MM/yyyy}" : null,
        Primary?.Deadline is DateTime due ? $"Prazo da regra: {due:dd/MM/yyyy}" : null,
        string.Join("\n", Evaluations.Select(e => $"{e.Label}: {e.Progress}" +
            (e.Deadline.HasValue && e != Primary ? $" Prazo: {e.Deadline:dd/MM/yyyy}." : ""))),
        Evaluations.Any(e => e.IncompleteHistory) ? "Há registros sem vínculo histórico à linha de cuidado; não comprovam o indicador." : null
    }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
