using SQLite;
namespace ACS_View.Domain.Entities;

public sealed class FamilyRelationship
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int FromPatientId { get; set; }
    public int ToPatientId { get; set; }
    public string TypeCode { get; set; } = "parent";
    public string ForwardLabel { get; set; } = "Pai/Mãe";
    public string ReverseLabel { get; set; } = "Filho/Filha";
    public string Nature { get; set; } = "Não informado";
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string EndReason { get; set; } = "Não informado";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Revision { get; set; }
    public FamilyRelationship Copy() => (FamilyRelationship)MemberwiseClone();
}
public sealed class FamilyRelationshipType
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Unique] public string Code { get; set; } = "";
    public string Category { get; set; } = "";
    public string ForwardLabel { get; set; } = "";
    public string ReverseLabel { get; set; } = "";
    public bool Symmetric { get; set; }
    public int Generation { get; set; }
}
