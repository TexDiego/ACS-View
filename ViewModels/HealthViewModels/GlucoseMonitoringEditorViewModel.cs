using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ACS_View.ViewModels.HealthViewModels;
public partial class GlucoseMonitoringEditorViewModel : ObservableObject
{
    [ObservableProperty] public partial string ChecksPerDay { get; set; } = "";
    [ObservableProperty] public partial bool VariableFrequency { get; set; }
    [ObservableProperty] public partial string StripsPerPickup { get; set; } = "";
    [ObservableProperty] public partial string PickupInterval { get; set; } = "Não informado";
    [ObservableProperty] public partial string CustomIntervalDays { get; set; } = "";
    [ObservableProperty] public partial bool HasLastKnownPickup { get; set; }
    [ObservableProperty] public partial DateTime LastKnownPickup { get; set; } = DateTime.Today;
    [ObservableProperty] public partial string MonitoringMethod { get; set; } = "Não informado";
    [ObservableProperty] public partial string Notes { get; set; } = "";
    public string[] Methods => GlucoseMonitoringRules.Methods;
    public string[] Intervals => ["Não informado", "Semanal", "Quinzenal", "Mensal", "Personalizada"];
    public DateTime Today => DateTime.Today;
    public bool HasValues => !string.IsNullOrWhiteSpace(ChecksPerDay) || VariableFrequency || !string.IsNullOrWhiteSpace(StripsPerPickup) || PickupInterval != "Não informado" || HasLastKnownPickup || MonitoringMethod != "Não informado" || !string.IsNullOrWhiteSpace(Notes);
    public void Load(PatientGlucoseMonitoring? m)
    {
        ChecksPerDay = m?.ChecksPerDay?.ToString() ?? ""; VariableFrequency = m?.VariableFrequency ?? false;
        StripsPerPickup = m?.StripsPerPickup?.ToString() ?? "";
        PickupInterval = m?.PickupIntervalDays switch { null => "Não informado", 7 => "Semanal", 15 => "Quinzenal", 30 => "Mensal", _ => "Personalizada" };
        CustomIntervalDays = m?.PickupIntervalDays?.ToString() ?? "";
        HasLastKnownPickup = m?.LastKnownPickup.HasValue ?? false; LastKnownPickup = m?.LastKnownPickup ?? DateTime.Today;
        MonitoringMethod = m?.MonitoringMethod ?? "Não informado"; Notes = m?.Notes ?? "";
    }
    private static int? Parse(string input, string name)
    {
        if(string.IsNullOrWhiteSpace(input)) return null;
        if(!int.TryParse(input, out var value) || value <= 0) throw new ArgumentException($"{name}: informe um inteiro positivo ou deixe vazio.");
        return value;
    }
    public PatientGlucoseMonitoring Build()
    {
        var value = new PatientGlucoseMonitoring { ChecksPerDay = Parse(ChecksPerDay, "Aferições/dia"), VariableFrequency = VariableFrequency,
            StripsPerPickup = Parse(StripsPerPickup, "Fitas por retirada"),
            PickupIntervalDays = PickupInterval switch { "Semanal" => 7, "Quinzenal" => 15, "Mensal" => 30, "Personalizada" => Parse(CustomIntervalDays, "Intervalo em dias") ?? throw new ArgumentException("Informe o intervalo personalizado."), _ => null },
            LastKnownPickup = HasLastKnownPickup ? LastKnownPickup.Date : null, MonitoringMethod = MonitoringMethod, Notes = Notes.Trim() };
        GlucoseMonitoringRules.Validate(value); return value;
    }
}
