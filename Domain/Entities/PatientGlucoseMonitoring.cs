using SQLite;
namespace ACS_View.Domain.Entities;

public sealed class PatientGlucoseMonitoring
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int PatientId { get; set; }
    public int? ChecksPerDay { get; set; }
    public bool VariableFrequency { get; set; }
    public int? StripsPerPickup { get; set; }
    public int? PickupIntervalDays { get; set; }
    public DateTime? LastKnownPickup { get; set; }
    public string MonitoringMethod { get; set; } = "Não informado";
    public string Notes { get; set; } = "";
}

public sealed class PatientStripPickup
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int PatientId { get; set; }
    public DateTime PickupDate { get; set; }
    public int Quantity { get; set; }
    public string Notes { get; set; } = "";
    [Ignore] public string DisplayText => $"{PickupDate:dd/MM/yyyy} · {Quantity} fitas" + (string.IsNullOrWhiteSpace(Notes) ? "" : $" — {Notes}");
}
