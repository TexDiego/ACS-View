namespace ACS_View.Application.DTOs;

public class HouseImportResultDto
{
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int IgnoredCount { get; set; }
    public List<string> Errors { get; set; } = [];

    public int TotalProcessed => ImportedCount + UpdatedCount + IgnoredCount;
}
