namespace ACS_View.Application.DTOs;

public sealed class ImportAuditEntryDto
{
    public string EntityType { get; set; } = "Paciente";
    public int RowNumber { get; set; }
    public int? RecordId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
    public string DisplayText => $"{Status} · {EntityType} · {(RowNumber > 0 ? $"Linha {RowNumber}" : "Cadastro relacionado")} · {Description}" +
        (RecordId is { } id ? $" · Cadastro #{id}" : "") + $"\n{Reason}" +
        (string.IsNullOrWhiteSpace(Changes) ? "" : $"\n{Changes}");

    public static void IncludeFailures(List<ImportAuditEntryDto> entries, IEnumerable<string> errors, string entityType = "Paciente")
    {
        foreach (var group in errors.Select(error => new
        {
            Error = error,
            Match = System.Text.RegularExpressions.Regex.Match(error, @"^Linha (\d+):")
        }).Where(item => item.Match.Success).GroupBy(item => int.Parse(item.Match.Groups[1].Value)))
        {
            if (entries.Any(entry => entry.RowNumber == group.Key)) continue;
            entries.Add(new() { EntityType = entityType, RowNumber = group.Key, Status = "Ignorado", Reason = string.Join("\n", group.Select(item => item.Error)) });
        }
    }
}
