namespace ACS_View.Application.DTOs;

/// <summary>Consolidates residence outcomes across all files in one import batch.</summary>
public sealed class ResidenceImportSummaryDto
{
    private readonly Dictionary<int, string> outcomes = [];
    private int unidentifiedIgnored;
    public int ImportedCount => outcomes.Values.Count(status => status == "Importado");
    public int UpdatedCount => outcomes.Values.Count(status => status == "Mesclado");
    public int IgnoredCount => outcomes.Values.Count(status => status == "Ignorado") + unidentifiedIgnored;

    public void Include(IEnumerable<ImportAuditEntryDto> entries)
    {
        foreach (var entry in entries.Where(e => e.EntityType == "Residência"))
        {
            if (entry.Status is not ("Importado" or "Mesclado" or "Ignorado")) continue;
            if (entry.RecordId is not > 0)
            {
                if (entry.Status == "Ignorado") unidentifiedIgnored++;
                continue;
            }
            var id = entry.RecordId.Value;
            if (!outcomes.TryGetValue(id, out var previous) || Priority(entry.Status) > Priority(previous))
                outcomes[id] = entry.Status;
        }
    }

    public void Include(HouseImportResultDto result)
    {
        Include(result.Entries);
        // Some invalid rows have no structured entry until the history is saved.
        unidentifiedIgnored += Math.Max(0, result.IgnoredCount - result.Entries.Count(e => e.EntityType == "Residência" && e.Status == "Ignorado"));
    }

    private static int Priority(string status) => status == "Importado" ? 3 : status == "Mesclado" ? 2 : 1;
}
