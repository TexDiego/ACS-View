using ACS_View.Domain.Enums;
using SQLite;

namespace ACS_View.Domain.Entities;

public class PatientPregnancy
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    [Indexed]
    public int PatientId { get; set; }

    public DateTime? LastMenstrualPeriod { get; set; }
    public DateTime? ExpectedBirthDate { get; set; }
    public PregnancyStatus Status { get; set; } = PregnancyStatus.Active;
    public PregnancyRisk ManualRisk { get; set; } = PregnancyRisk.NotInformed;
    public int? InformedChildrenCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public PregnancyEndType? EndType { get; set; }
    public string Notes { get; set; } = string.Empty;
}
