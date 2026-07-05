using SQLite;

namespace ACS_View.Domain.Entities;

public class VisitBatch
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HouseId { get; set; }
    public int FamilyId { get; set; }
    public DateTime VisitDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}
