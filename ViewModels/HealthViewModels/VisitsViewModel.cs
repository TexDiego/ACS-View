using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS_View.ViewModels.HealthViewModels;

public sealed record VisitMonth(string Month, string Counts, string Details);

public sealed record VisitIndicator(VisitRuleEvaluation Evaluation)
{
    public string Title => Evaluation.Label;
    public string Count => $"{Evaluation.Completed} / {Evaluation.Required}";
    public double Progress => Evaluation.Required > 0 ? Math.Clamp((double)Evaluation.Completed / Evaluation.Required, 0, 1) : 0;
    public string Status => Evaluation.StatusText;
    public string Explanation => string.Join("\n", new[]
    {
        Evaluation.Progress,
        Evaluation.Deadline is DateTime due ? $"Prazo: {due:dd/MM/yyyy}" : null,
        Evaluation.Source,
        string.Join("\n", Evaluation.Milestones.Select(m => $"{m.Name}: {m.Deadline:dd/MM/yyyy} · {(m.Completed ? "Concluído" : "Pendente")}"))
    }.Where(text => !string.IsNullOrWhiteSpace(text)));
}

public partial class VisitsViewModel(PatientHealthSession session, IPatientHealthDataService data)
    : HealthModuleViewModel(session, "visits")
{
    [ObservableProperty] public partial string NextVisit { get; set; } = "Sem data sugerida";
    [ObservableProperty] public partial string RecommendationStatus { get; set; } = "";
    [ObservableProperty] public partial string LastVisit { get; set; } = "Sem registro";
    [ObservableProperty] public partial string MonthSummary { get; set; } = "";
    [ObservableProperty] public partial string LastAttempt { get; set; } = "";
    [ObservableProperty] public partial string DataNote { get; set; } = "";
    [ObservableProperty] public partial IReadOnlyList<VisitIndicator> Indicators { get; set; } = [];
    [ObservableProperty] public partial IReadOnlyList<VisitMonth> Months { get; set; } = [];
    [ObservableProperty] public partial VisitMonth? SelectedMonth { get; set; }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CriteriaButtonText))]
    public partial bool ShowCriteria { get; set; }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HistoryButtonText))]
    public partial bool ShowHistoryRecords { get; set; }

    public string CriteriaButtonText => ShowCriteria ? "Ocultar critérios ↑" : "Entender os indicadores ↓";
    public string HistoryButtonText => ShowHistoryRecords ? "Ocultar registros ↑" : "Ver registros do mês ↓";
    public bool HasHistory => Months.Count > 0;
    public bool HasNoHistory => !HasHistory;
    public bool HasIndicators => Indicators.Count > 0;
    public bool HasDataNote => !string.IsNullOrWhiteSpace(DataNote);
    public bool HasLastAttempt => !string.IsNullOrWhiteSpace(LastAttempt);

    [RelayCommand] private void ToggleCriteria() => ShowCriteria = !ShowCriteria;
    [RelayCommand] private void ToggleHistoryRecords() => ShowHistoryRecords = !ShowHistoryRecords;
    partial void OnSelectedMonthChanged(VisitMonth? value) => ShowHistoryRecords = false;

    protected override async Task LoadDataAsync()
    {
        var value = await data.GetVisitsAsync(Session);
        EnsureActive();
        var summary = value.Summary;
        NextVisit = summary.Primary?.RecommendedDate?.ToString("dd/MM/yyyy") ?? "Sem data sugerida";
        RecommendationStatus = summary.Primary?.StatusText ?? "Sem recomendação específica";
        LastVisit = summary.LastCompletedVisit?.ToString("dd/MM/yyyy") ?? "Sem registro";
        var month = summary.ReferenceMonth == default ? DateTime.Today : summary.ReferenceMonth;
        MonthSummary = $"{summary.MonthlyCompletedVisits} realizada(s) em {month:MM/yyyy}";
        LastAttempt = summary.LastAttempt is DateTime attempt && attempt > summary.LastCompletedVisit.GetValueOrDefault()
            && summary.LastAttemptStatus != "Completed"
            ? $"Última tentativa: {attempt:dd/MM/yyyy} · {(summary.LastAttemptStatus == "Absent" ? "Ausente" : "Recusada")}" : "";
        DataNote = string.Join("\n", new[] { summary.DataQualityNote,
            summary.Evaluations.Any(e => e.IncompleteHistory) ? "Há registros sem vínculo histórico à linha de cuidado; não comprovam o indicador." : null
        }.Where(text => !string.IsNullOrWhiteSpace(text)));
        Indicators = summary.Evaluations.Select(e => new VisitIndicator(e)).ToList();
        var selected = SelectedMonth?.Month;
        var wasExpanded = ShowHistoryRecords;
        Months = value.Records.GroupBy(v => new DateTime(v.VisitDate.Year, v.VisitDate.Month, 1)).OrderByDescending(g => g.Key)
            .Select(g => new VisitMonth(g.Key.ToString("MM/yyyy"),
                $"Realizadas: {g.Count(v => v.Status == "Completed")} · Ausentes: {g.Count(v => v.Status == "Absent")} · Recusadas: {g.Count(v => v.Status == "Refused")}",
                string.Join("\n", g.Select(v => $"{v.VisitDate:dd/MM/yyyy} · {(v.Status == "Completed" ? "Realizada" : v.Status == "Absent" ? "Ausente" : v.Status == "Refused" ? "Recusada" : v.Description)}{(string.IsNullOrWhiteSpace(v.Notes) ? "" : " — " + v.Notes)}")))).ToList();
        SelectedMonth = Months.FirstOrDefault(m => m.Month == selected) ?? Months.FirstOrDefault();
        ShowHistoryRecords = wasExpanded && SelectedMonth is not null;
        foreach (var property in new[] { nameof(HasHistory), nameof(HasNoHistory), nameof(HasIndicators), nameof(HasDataNote), nameof(HasLastAttempt) })
            OnPropertyChanged(property);
    }
}
