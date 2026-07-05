using SQLite;

namespace ACS_View.Domain.Entities;

public class VisitCareLine
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int UserId { get; set; }
    public int VisitId { get; set; }
    public string CareLineType { get; set; } = string.Empty;
}
