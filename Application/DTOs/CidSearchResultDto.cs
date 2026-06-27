namespace ACS_View.Application.DTOs;

public sealed class CidSearchResultDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryDescription { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string GroupDescription { get; set; } = string.Empty;
    public string ChapterCode { get; set; } = string.Empty;
    public string ChapterDescription { get; set; } = string.Empty;
    public string DisplayTitle => $"{Code} - {Description}";
    public string HierarchySummary
    {
        get
        {
            var summary = string.Join(" | ", new[] { ChapterDescription, GroupDescription }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            return string.IsNullOrWhiteSpace(summary)
                ? "Hierarquia nao informada"
                : summary;
        }
    }
}
