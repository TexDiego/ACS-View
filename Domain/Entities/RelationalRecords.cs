using SQLite;

namespace ACS_View.Domain.Entities;

// Family is kept as the legacy table; FamilyRecord carries the independent identity.
public sealed class FamilyRecord
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int? HouseId { get; set; }
    public int? ResponsiblePatientId { get; set; }
    public int? LocalNumber { get; set; }
    public DateTime? CreatedAt { get; set; }
}
public sealed class FamilyMember
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int FamilyId { get; set; }
    public int PatientId { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
public sealed class FamilyResidenceHistory
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int FamilyId { get; set; }
    public int HouseId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
public sealed class PatientPhone
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int PatientId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? NormalizedNumber { get; set; }
    public string Type { get; set; } = "Não informado";
    public bool HasWhatsApp { get; set; }
    public string? Notes { get; set; }
    public string? ContactName { get; set; }
    public string? Relationship { get; set; }
    public bool IsPrimary { get; set; }
}
public sealed class TerritorialAssignment
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int HouseId { get; set; }
    public string CensusSector { get; set; } = string.Empty;
    public string BlockNumber { get; set; } = string.Empty;
    public string AreaCode { get; set; } = string.Empty;
    public string SectorCode { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
public sealed class MigrationIssue
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public string SourceTable { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string Code { get; set; } = string.Empty;
    // Evidence stays in the private local database, never in diagnostic logs.
    public string Evidence { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
