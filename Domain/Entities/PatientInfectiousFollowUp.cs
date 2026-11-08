using SQLite;
namespace ACS_View.Domain.Entities;

public sealed class PatientInfectiousFollowUp
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int PatientId { get; set; }
    public string ConditionCode { get; set; } = "";
    public string Status { get; set; } = "Não informado";
    public DateTime? TreatmentStarted { get; set; }
    public DateTime? LastAppointment { get; set; }
    public DateTime? NextAppointment { get; set; }
    public string ReferenceService { get; set; } = "";
    public string Notes { get; set; } = "";
}
