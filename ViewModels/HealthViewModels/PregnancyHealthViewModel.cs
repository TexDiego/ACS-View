using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS_View.ViewModels.HealthViewModels;

public sealed partial class PregnancyHealthViewModel(PatientHealthSession session, IPatientHealthDataService data)
    : ClinicalModuleViewModel(session, data)
{
    [ObservableProperty] public partial bool HasData { get; set; }
    [ObservableProperty] public partial string Stage { get; set; } = "Gestação e puerpério";
    [ObservableProperty] public partial string Age { get; set; } = "Não informada";
    [ObservableProperty] public partial string AgeLabel { get; set; } = "Idade gestacional";
    [ObservableProperty] public partial string Period { get; set; } = "";
    [ObservableProperty] public partial string DateLabel { get; set; } = "Data provável do parto";
    [ObservableProperty] public partial string ReferenceDate { get; set; } = "Não informada";
    [ObservableProperty] public partial string ManualRisk { get; set; } = "Não informado";
    [ObservableProperty] public partial string SuggestedRisk { get; set; } = "Não informado";
    [ObservableProperty] public partial string RiskReasons { get; set; } = "";
    [ObservableProperty] public partial string RecordedDetails { get; set; } = "";
    [ObservableProperty] public partial string Notes { get; set; } = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(RiskButtonText))]
    public partial bool ShowRisk { get; set; }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(RecordsButtonText))]
    public partial bool ShowRecords { get; set; }
    public string RiskButtonText => ShowRisk ? "Ocultar avaliação de risco ↑" : "Ver avaliação de risco ↓";
    public string RecordsButtonText => ShowRecords ? "Ocultar dados e observações ↑" : "Ver dados e observações ↓";
    public bool HasNoData => !HasData;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    [RelayCommand] private void ToggleRisk() => ShowRisk = !ShowRisk;
    [RelayCommand] private void ToggleRecords() => ShowRecords = !ShowRecords;

    protected override string Describe(ClinicalHealthData value)
    {
        var d = value.Pregnancy;
        HasData = d is not null;
        OnPropertyChanged(nameof(HasNoData));
        if (d is null) return "";
        var p = d.Pregnancy;
        var postpartum = PregnancyCalculator.IsPuerperal(p);
        Stage = postpartum ? "Puérpera" : p.Status switch
        {
            PregnancyStatus.Active => "Gestante",
            PregnancyStatus.Ended => "Gestação encerrada",
            _ => "Não confirmada"
        };
        AgeLabel = postpartum ? "Tempo pós-parto" : "Idade gestacional";
        Age = postpartum ? $"{PregnancyCalculator.CalculatePostpartumDays(p)} dias" : d.GestationalAge?.ToString() ?? "Não informada";
        Period = !postpartum && d.Trimester.HasValue ? $"{d.Trimester}º trimestre" : "";
        DateLabel = postpartum ? "Fim do puerpério" : "Data provável do parto";
        ReferenceDate = (postpartum ? PregnancyCalculator.CalculatePuerperiumEndDate(p) : p.ExpectedBirthDate)?.ToString("dd/MM/yyyy") ?? "Não informada";
        ManualRisk = d.RiskText;
        SuggestedRisk = d.SuggestedRiskText;
        RiskReasons = d.RiskSuggestion.ReasonsText;
        RecordedDetails = string.Join("\n", new[]
        {
            $"DUM: {p.LastMenstrualPeriod?.ToString("dd/MM/yyyy") ?? "Não informada"}",
            $"Filhos informados: {p.InformedChildrenCount?.ToString() ?? "Não informado"}",
            p.EndedAt.HasValue ? $"Encerramento: {p.EndedAt:dd/MM/yyyy}" : null
        }.Where(x => x is not null));
        Notes = p.Notes;
        OnPropertyChanged(nameof(HasNotes));
        return "";
    }
}
