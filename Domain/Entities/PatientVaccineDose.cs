using SQLite;

namespace ACS_View.Domain.Entities;

public class PatientVaccineDose
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    [Indexed]
    public int PatientId { get; set; }

    [Indexed]
    public string DoseKey { get; set; } = string.Empty;

    public bool IsApplied { get; set; }
    public DateTime? AppliedAt { get; set; }
}
