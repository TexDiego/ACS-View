using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ACS_View.UseCases.Services;

internal sealed class SpreadsheetReader : ISpreadsheetReader
{
    private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public List<List<string>> ReadWorksheetRows(Stream fileStream)
    {
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheetEntry = GetFirstWorksheetEntry(archive)
            ?? throw new InvalidDataException("Nao foi possivel encontrar a primeira aba da planilha.");

        using var worksheetStream = worksheetEntry.Open();
        var worksheet = XDocument.Load(worksheetStream);
        var rows = new List<List<string>>();

        foreach (var rowElement in worksheet.Descendants(SpreadsheetNamespace + "row"))
        {
            var values = new List<string>();
            var nextColumnIndex = 0;

            foreach (var cell in rowElement.Elements(SpreadsheetNamespace + "c"))
            {
                var cellReference = cell.Attribute("r")?.Value ?? string.Empty;
                var columnIndex = TryGetColumnIndex(cellReference) ?? nextColumnIndex;

                if (columnIndex < nextColumnIndex && HasValueAt(values, columnIndex))
                {
                    columnIndex = nextColumnIndex;
                }

                while (values.Count <= columnIndex)
                {
                    values.Add(string.Empty);
                }

                values[columnIndex] = ReadCellValue(cell, sharedStrings);
                nextColumnIndex = columnIndex + 1;
            }

            if (values.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                rows.Add(values);
            }
        }

        return rows;
    }

    private static ZipArchiveEntry? GetFirstWorksheetEntry(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml");
        var relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");

        if (workbookEntry is null || relationshipsEntry is null)
        {
            return archive.GetEntry("xl/worksheets/sheet1.xml");
        }

        using var workbookStream = workbookEntry.Open();
        using var relationshipsStream = relationshipsEntry.Open();
        var workbook = XDocument.Load(workbookStream);
        var relationships = XDocument.Load(relationshipsStream);
        var firstSheet = workbook.Descendants(SpreadsheetNamespace + "sheet").FirstOrDefault();
        var relationshipId = firstSheet?.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"))?.Value;

        if (string.IsNullOrWhiteSpace(relationshipId))
        {
            return archive.GetEntry("xl/worksheets/sheet1.xml");
        }

        var relationship = relationships.Root?
            .Elements()
            .FirstOrDefault(element => element.Attribute("Id")?.Value == relationshipId);
        var target = relationship?.Attribute("Target")?.Value;

        if (string.IsNullOrWhiteSpace(target))
        {
            return archive.GetEntry("xl/worksheets/sheet1.xml");
        }

        var normalizedTarget = target.Replace('\\', '/').TrimStart('/');
        var worksheetPath = normalizedTarget.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
            ? normalizedTarget
            : $"xl/{normalizedTarget}";

        return archive.GetEntry(worksheetPath) ?? archive.GetEntry("xl/worksheets/sheet1.xml");
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedStringsEntry is null)
        {
            return [];
        }

        using var stream = sharedStringsEntry.Open();
        var document = XDocument.Load(stream);

        return document.Descendants(SpreadsheetNamespace + "si")
            .Select(item => string.Concat(item.Descendants(SpreadsheetNamespace + "t").Select(text => text.Value)))
            .ToList();
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;

        if (type == "inlineStr")
        {
            return string.Concat(cell.Descendants(SpreadsheetNamespace + "t").Select(text => text.Value));
        }

        var rawValue = cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty;
        if (type == "s" && int.TryParse(rawValue, out var sharedStringIndex) && sharedStringIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedStringIndex];
        }

        return rawValue;
    }

    private static bool HasValueAt(IReadOnlyList<string> values, int index)
    {
        return index >= 0 && index < values.Count && !string.IsNullOrWhiteSpace(values[index]);
    }

    private static int? TryGetColumnIndex(string cellReference)
    {
        var letters = Regex.Match(cellReference, "^[A-Z]+", RegexOptions.IgnoreCase).Value.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(letters))
        {
            return null;
        }

        var columnIndex = 0;

        foreach (var letter in letters)
        {
            columnIndex *= 26;
            columnIndex += letter - 'A' + 1;
        }

        return Math.Max(0, columnIndex - 1);
    }
}
