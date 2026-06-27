namespace ACS_View.UseCases.Services;

internal interface ISpreadsheetReader
{
    List<List<string>> ReadWorksheetRows(Stream fileStream);
}
