using SQLite;

namespace ACS_View.Domain.Entities;

public class ImportHistory
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StartedAt { get; set; } = string.Empty;
    public string FinishedAt { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReportJson { get; set; } = string.Empty;
}
