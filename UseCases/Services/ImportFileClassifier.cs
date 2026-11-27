using ACS_View.Domain.ValueObjects;

namespace ACS_View.UseCases.Services;

internal static class ImportFileClassifier
{
    // Patient files can also contain addresses: preserve the patient importer,
    // which resolves those addresses and creates complete residences itself.
    public static bool? Detect(Stream input)
    {
        var rows = new SpreadsheetReader().ReadWorksheetRows(input).Take(20).ToList();
        if (rows.Any(row => row.Any(cell => ImportColumnAliases.Matches(cell, "Nome")))) return true;
        if (rows.Any(row => row.Any(cell => ImportColumnAliases.Matches(cell, "Rua") || ImportColumnAliases.Matches(cell, "CEP")))) return false;
        return null;
    }
}
