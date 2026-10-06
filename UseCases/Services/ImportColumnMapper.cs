using ACS_View.Application.DTOs;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.UseCases.Services;

internal static class ImportColumnMapper
{
    public static Dictionary<string, int> Map(List<List<string>> rows, int headerIndex,
        object map, List<string> errors, List<string> details)
    {
        var header = rows[headerIndex].ToList();
        var columns = map.GetType().GetProperties()
            .Where(p => p.Name.EndsWith("Column") && p.PropertyType == typeof(string))
            .Select(p => (string?)p.GetValue(map) ?? "")
            .Concat(map is PatientImportColumnMapDto patientMap ? patientMap.HealthConditionColumns.Select(c => c.ColumnName) : []).Where(c => c.Length > 0).Distinct().ToList();
        foreach (var column in columns)
        {
            var indexes = Enumerable.Range(0, header.Count).Where(i => ImportColumnAliases.Matches(header[i], column)).ToList();
            if (indexes.Count == 0) continue;
            var target = rows[headerIndex].Count;
            rows[headerIndex].Add(column);
            details.Add($"Coluna {column}: {string.Join(", ", indexes.Select(i => header[i]))}.");
            for (var r = headerIndex + 1; r < rows.Count; r++)
            {
                var values = indexes.Select(i => GetCell(rows[r], i).Trim()).Where(v => v.Length > 0).Distinct().ToList();
                while (rows[r].Count <= target) rows[r].Add("");
                rows[r][target] = values.FirstOrDefault() ?? "";
                if (values.Count > 1) errors.Add($"Linha {r + 1}: colunas equivalentes de {column} divergem ({string.Join(" / ", values)}); usado '{values[0]}'.");
            }
        }
        // Synthetic columns contain the first nonempty alias value for each row.
        var resultMap = new Dictionary<string, int>();
        for (var i = 0; i < rows[headerIndex].Count; i++) resultMap[Normalize(rows[headerIndex][i])] = i;
        return resultMap;
    }

    private static string Normalize(string value) => ImportColumnAliases.Normalize(value);
    private static string GetCell(IReadOnlyList<string> row, int index) => index < row.Count ? row[index] : "";
}
