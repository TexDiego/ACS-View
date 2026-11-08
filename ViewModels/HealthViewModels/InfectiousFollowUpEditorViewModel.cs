using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace ACS_View.ViewModels.HealthViewModels;

public partial class InfectiousFollowUpEditorViewModel(string conditionCode) : ObservableObject
{
    [ObservableProperty] public partial string Status { get; set; } = "Não informado";
    [ObservableProperty] public partial bool HasTreatmentStarted { get; set; }
    [ObservableProperty] public partial DateTime TreatmentStarted { get; set; } = DateTime.Today;
    [ObservableProperty] public partial bool HasLastAppointment { get; set; }
    [ObservableProperty] public partial DateTime LastAppointment { get; set; } = DateTime.Today;
    [ObservableProperty] public partial bool HasNextAppointment { get; set; }
    [ObservableProperty] public partial DateTime NextAppointment { get; set; } = DateTime.Today;
    [ObservableProperty] public partial string ReferenceService { get; set; } = "";
    [ObservableProperty] public partial string Notes { get; set; } = "";
    [ObservableProperty] public partial string Summary { get; set; } = "Acompanhamento não informado.";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DetailsButtonText))]
    public partial bool ShowDetails { get; set; }
    public string DetailsButtonText => ShowDetails ? "Recolher acompanhamento ↑" : "Informar / editar acompanhamento ↓";
    public string[] StatusOptions => InfectiousFollowUpRules.Statuses(conditionCode);
    public DateTime Today => DateTime.Today;
    public bool HasPersistedData { get; private set; }
    public bool HasValues => HasPersistedData || Status != "Não informado" || HasTreatmentStarted || HasLastAppointment || HasNextAppointment || !string.IsNullOrWhiteSpace(ReferenceService) || !string.IsNullOrWhiteSpace(Notes);
    [RelayCommand] private void ToggleDetails() => ShowDetails = !ShowDetails;
    public void Load(PatientInfectiousFollowUp? value)
    {
        HasPersistedData = value is not null;
        Status = value?.Status ?? "Não informado";
        HasTreatmentStarted = value?.TreatmentStarted.HasValue ?? false; TreatmentStarted = value?.TreatmentStarted ?? DateTime.Today;
        HasLastAppointment = value?.LastAppointment.HasValue ?? false; LastAppointment = value?.LastAppointment ?? DateTime.Today;
        HasNextAppointment = value?.NextAppointment.HasValue ?? false; NextAppointment = value?.NextAppointment ?? DateTime.Today;
        ReferenceService = value?.ReferenceService ?? ""; Notes = value?.Notes ?? "";
        Summary = value is null ? "Acompanhamento não informado." : string.Join("\n", new[]
        {
            value.Status,
            value.TreatmentStarted is DateTime start ? $"Tratamento iniciado em {start:dd/MM/yyyy}" : null,
            value.LastAppointment is DateTime last ? $"Última consulta: {last:dd/MM/yyyy}" : null,
            value.NextAppointment is DateTime next ? $"Próxima consulta informada: {next:dd/MM/yyyy}" : null,
            string.IsNullOrWhiteSpace(value.ReferenceService) ? null : $"Serviço: {value.ReferenceService}", value.Notes
        }.Where(text => !string.IsNullOrWhiteSpace(text)));
    }
    public PatientInfectiousFollowUp Build()
    {
        var value = new PatientInfectiousFollowUp
        {
            ConditionCode = conditionCode, Status = Status,
            TreatmentStarted = HasTreatmentStarted ? TreatmentStarted.Date : null,
            LastAppointment = HasLastAppointment ? LastAppointment.Date : null,
            NextAppointment = HasNextAppointment ? NextAppointment.Date : null,
            ReferenceService = ReferenceService.Trim(), Notes = Notes.Trim()
        };
        InfectiousFollowUpRules.Validate(value); return value;
    }
}
