using SQLite;
namespace ACS_View.Domain.Entities;
public sealed class PatientSusNumber
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int UserId { get; set; }
    public int PatientId { get; set; }
    public string Number { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int OriginalOrder { get; set; }
}
