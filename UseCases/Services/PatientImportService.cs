using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.Application.DTOs;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ACS_View.UseCases.Services
{
    internal class PatientImportService(
        IPatientService patientService,
        ISQLiteConditionsRepository conditionsRepository) : IPatientImportService
    {
        private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        public async Task<PatientImportResultDto> ImportAsync(Stream fileStream, PatientImportColumnMapDto columnMap)
        {
            var result = new PatientImportResultDto();
            var rows = ReadWorksheetRows(fileStream);

            if (rows.Count == 0)
            {
                result.Errors.Add("A planilha não possui linhas para importar.");
                return result;
            }

            var headerMatch = FindHeaderRow(rows, columnMap.NameColumn);
            if (headerMatch is null)
            {
                var detectedColumns = rows.Count > 0
                    ? string.Join(", ", rows[0].Where(value => !string.IsNullOrWhiteSpace(value)).Take(12))
                    : string.Empty;

                result.Errors.Add($"Não encontrei a coluna \"{columnMap.NameColumn}\". Colunas lidas: {detectedColumns}");
                return result;
            }

            var headerMap = headerMatch.Value.HeaderMap;
            var nameIndex = FindColumnIndex(headerMap, columnMap.NameColumn);
            var susIndex = FindColumnIndex(headerMap, columnMap.SusNumberColumn);
            var motherIndex = FindColumnIndex(headerMap, columnMap.MotherNameColumn);
            var fatherIndex = FindColumnIndex(headerMap, columnMap.FatherNameColumn);
            var birthDateIndex = FindColumnIndex(headerMap, columnMap.BirthDateColumn);
            var observationIndex = FindColumnIndex(headerMap, columnMap.ObservationColumn);
            var bolsaFamiliaIndex = FindColumnIndex(headerMap, columnMap.BolsaFamiliaColumn);
            var conditionColumnIndexes = columnMap.HealthConditionColumns
                .Where(map => !string.IsNullOrWhiteSpace(map.ConditionName) && !string.IsNullOrWhiteSpace(map.ColumnName))
                .Select(map => new
                {
                    ConditionName = map.ConditionName,
                    ColumnIndex = FindColumnIndex(headerMap, map.ColumnName)
                })
                .Where(map => map.ColumnIndex is not null)
                .ToList();

            var existingPatients = await patientService.GetAllPatients() ?? [];
            var patientsBySus = existingPatients
                .Where(patient => !string.IsNullOrWhiteSpace(patient.SusNumber))
                .GroupBy(patient => Normalize(patient.SusNumber))
                .ToDictionary(group => group.Key, group => group.First());

            for (var rowIndex = headerMatch.Value.RowIndex + 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var name = GetCell(row, nameIndex!.Value);

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.IgnoredCount++;
                    continue;
                }

                var susNumber = GetCell(row, susIndex);
                var patient = !string.IsNullOrWhiteSpace(susNumber) &&
                              patientsBySus.TryGetValue(Normalize(susNumber), out var existing)
                    ? existing
                    : new Patient();

                patient.Name = name.Trim();
                patient.SusNumber = susNumber.Trim();
                patient.MotherName = GetCell(row, motherIndex).Trim();
                patient.FatherName = GetCell(row, fatherIndex).Trim();
                patient.Observacao = GetCell(row, observationIndex).Trim();

                var importedConditions = conditionColumnIndexes
                    .Where(map => ParseBoolean(GetCell(row, map.ColumnIndex)))
                    .Select(map => map.ConditionName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                patient.BolsaFamilia = ParseBoolean(GetCell(row, bolsaFamiliaIndex)) ||
                                       importedConditions.Any(condition =>
                                           HealthConditionCatalog.GetKey(condition) == HealthConditionCatalog.BolsaFamilia);

                var birthDate = ParseDate(GetCell(row, birthDateIndex));
                if (birthDate is not null)
                {
                    patient.BirthDate = birthDate.Value;
                }

                try
                {
                    var isExistingPatient = patient.Id > 0;
                    if (isExistingPatient)
                    {
                        await patientService.UpdatePatient(patient);
                    }
                    else
                    {
                        await patientService.CreatePatient(patient);
                    }

                    if (!string.IsNullOrWhiteSpace(patient.SusNumber))
                    {
                        patientsBySus[Normalize(patient.SusNumber)] = patient;
                    }

                    await SyncImportedConditionsAsync(patient.Id, conditionColumnIndexes.Select(map => map.ConditionName), importedConditions);

                    if (isExistingPatient)
                    {
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.ImportedCount++;
                    }
                }
                catch (ArgumentException ex)
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: não foi possível importar o paciente. {ex.Message}");
                }
            }

            return result;
        }

        private async Task SyncImportedConditionsAsync(
            int patientId,
            IEnumerable<string> mappedConditionNames,
            IEnumerable<string> selectedConditionNames)
        {
            var mappedKeys = mappedConditionNames
                .Select(HealthConditionCatalog.GetKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (mappedKeys.Count == 0)
            {
                return;
            }

            var currentConditions = await conditionsRepository.GetConditionsByPatientIdAsync(patientId);
            foreach (var condition in currentConditions.Where(condition => mappedKeys.Contains(HealthConditionCatalog.GetKey(condition.Description))))
            {
                await conditionsRepository.DeleteConditionAsync(condition.Id);
            }

            foreach (var conditionName in selectedConditionNames)
            {
                await conditionsRepository.InsertConditionAsync(new PatientConditions
                {
                    PatientId = patientId,
                    Description = conditionName
                });
            }
        }

        private static (int RowIndex, Dictionary<string, int> HeaderMap)? FindHeaderRow(
            IReadOnlyList<List<string>> rows,
            string requiredNameColumn)
        {
            var rowsToScan = Math.Min(rows.Count, 20);

            for (var rowIndex = 0; rowIndex < rowsToScan; rowIndex++)
            {
                var headerMap = BuildHeaderMap(rows[rowIndex]);
                if (FindColumnIndex(headerMap, requiredNameColumn) is not null)
                {
                    return (rowIndex, headerMap);
                }
            }

            return null;
        }

        private static List<List<string>> ReadWorksheetRows(Stream fileStream)
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
            var sharedStrings = ReadSharedStrings(archive);
            var worksheetEntry = GetFirstWorksheetEntry(archive)
                ?? throw new InvalidDataException("Não foi possível encontrar a primeira aba da planilha.");

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

        private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> header)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < header.Count; index++)
            {
                var normalized = Normalize(header[index]);
                if (!string.IsNullOrWhiteSpace(normalized) && !map.ContainsKey(normalized))
                {
                    map[normalized] = index;
                }
            }

            return map;
        }

        private static int? FindColumnIndex(IReadOnlyDictionary<string, int> headerMap, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                return null;
            }

            var normalizedColumnName = Normalize(columnName);
            if (headerMap.TryGetValue(normalizedColumnName, out var index))
            {
                return index;
            }

            var compactColumnName = Compact(normalizedColumnName);
            foreach (var item in headerMap)
            {
                var compactHeader = Compact(item.Key);
                if (compactHeader == compactColumnName ||
                    compactHeader.Contains(compactColumnName, StringComparison.OrdinalIgnoreCase) ||
                    compactColumnName.Contains(compactHeader, StringComparison.OrdinalIgnoreCase))
                {
                    return item.Value;
                }
            }

            return null;
        }

        private static string GetCell(IReadOnlyList<string> row, int? index)
        {
            if (index is null || index.Value < 0 || index.Value >= row.Count)
            {
                return string.Empty;
            }

            return row[index.Value];
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

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            if (DateTime.TryParse(trimmed, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var date) ||
                DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date.Date;
            }

            if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var serialDate))
            {
                try
                {
                    return DateTime.FromOADate(serialDate).Date;
                }
                catch (ArgumentException)
                {
                }
            }

            return null;
        }

        private static bool ParseBoolean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = Normalize(value);
            return normalized is "1" or "sim" or "s" or "true" or "verdadeiro" or "x";
        }

        private static string Normalize(string value)
        {
            var normalized = Regex.Replace(value.Replace('\u00A0', ' ').Trim(), @"\s+", " ").Normalize(NormalizationForm.FormD);
            var chars = normalized
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        private static string Compact(string value)
        {
            return Regex.Replace(value, @"[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase);
        }
    }
}
