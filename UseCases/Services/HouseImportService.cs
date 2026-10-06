using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ACS_View.UseCases.Services;

internal class HouseImportService(
    IHouseService houseService,
    ICepService cepService, ISpreadsheetReader spreadsheetReader, IImportHistoryService historyService) : IHouseImportService
{

    public async Task<HouseImportResultDto> ImportAsync(
        Stream fileStream,
        HouseImportColumnMapDto columnMap,
        IProgress<ImportProgressDto>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new HouseImportResultDto();
        var history = new ImportHistory { FileName = columnMap.SourceFileName, StartedAt = DateTimeOffset.Now.ToString("O"), Status = "Concluída" };
        try { return await ImportCoreAsync(fileStream, columnMap, result, progress, cancellationToken); }
        catch (OperationCanceledException) { history.Status = "Cancelada (dados já gravados foram mantidos)"; result.Errors.Add("Importação cancelada."); throw; }
        catch (Exception ex) { history.Status = "Interrompida"; result.Errors.Add(ex.Message); throw; }
        finally
        {
            history.FinishedAt = DateTimeOffset.Now.ToString("O");
            if (history.Status == "Concluída" && result.Errors.Count > 0) history.Status = "Concluída com pendências";
            history.ReportJson = System.Text.Json.JsonSerializer.Serialize(result);
            await historyService.SaveAsync(history);
        }
    }

    private async Task<HouseImportResultDto> ImportCoreAsync(Stream fileStream, HouseImportColumnMapDto columnMap,
        HouseImportResultDto result, IProgress<ImportProgressDto>? progress, CancellationToken cancellationToken)
    {
        Report(progress, 0, 1, "Lendo planilha");

        cancellationToken.ThrowIfCancellationRequested();
        var rows = spreadsheetReader.ReadWorksheetRows(fileStream);

        if (rows.Count == 0)
        {
            result.Errors.Add("A planilha nao possui linhas para importar.");
            return result;
        }

        var headerMatch = FindHeaderRow(rows, columnMap.StreetColumn, columnMap.CepColumn);
        if (headerMatch is null)
        {
            var detectedColumns = string.Join(", ", rows[0].Where(value => !string.IsNullOrWhiteSpace(value)).Take(12));
            result.Errors.Add($"Nao encontrei as colunas \"{columnMap.StreetColumn}\" ou \"{columnMap.CepColumn}\". Colunas lidas: {detectedColumns}");
            return result;
        }

        var headerMap = ImportColumnMapper.Map(rows, headerMatch.Value.RowIndex, columnMap, result.Errors, result.Details);
        var streetIndex = FindColumnIndex(headerMap, columnMap.StreetColumn);
        var streetTypeIndex = FindColumnIndex(headerMap, columnMap.StreetTypeColumn);
        var cepIndex = FindColumnIndex(headerMap, columnMap.CepColumn);
        var numberIndex = FindColumnIndex(headerMap, columnMap.NumberColumn);
        var neighborhoodIndex = FindColumnIndex(headerMap, columnMap.NeighborhoodColumn);
        var cityIndex = FindColumnIndex(headerMap, columnMap.CityColumn);
        var stateIndex = FindColumnIndex(headerMap, columnMap.StateColumn);
        var countryIndex = FindColumnIndex(headerMap, columnMap.CountryColumn);
        var complementIndex = FindColumnIndex(headerMap, columnMap.ComplementColumn);

        var existingHouses = await houseService.GetAllHousesAsync();
        var housesByKey = BuildHouseKeyIndex(existingHouses);
        var cepCache = new Dictionary<string, House?>();
        var dataRowCount = Math.Max(0, rows.Count - headerMatch.Value.RowIndex - 1);
        var totalProgressItems = Math.Max(1, dataRowCount + 2);
        var processedProgressItems = 1;

        Report(progress, processedProgressItems, totalProgressItems, "Importando residencias");

        for (var rowIndex = headerMatch.Value.RowIndex + 1; rowIndex < rows.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var row = rows[rowIndex];
                if (row.All(string.IsNullOrWhiteSpace)) continue;
                var cep = GetCell(row, cepIndex).Trim();
                var importedStreet = GetCell(row, streetIndex);
                var importedNeighborhood = GetCell(row, neighborhoodIndex);
                var importedCity = GetCell(row, cityIndex);
                var importedState = GetCell(row, stateIndex);
                var importedCountry = GetCell(row, countryIndex);
                var needsCepFallback = HasMissingAddressValue(
                    importedStreet,
                    importedNeighborhood,
                    importedCity,
                    importedState,
                    importedCountry);

                var addressFallback = needsCepFallback
                    ? await ResolveAddressFallbackAsync(cep, cepCache)
                    : null;

                cancellationToken.ThrowIfCancellationRequested();

                var street = CoalesceAddressValue(importedStreet, addressFallback?.Rua);
                if (string.IsNullOrWhiteSpace(street))
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: nao foi possivel identificar a rua. Informe a coluna de rua ou um CEP valido.");
                    processedProgressItems = ReportEvery(
                        progress,
                        processedProgressItems + 1,
                        totalProgressItems,
                        "Importando residencias",
                        rowIndex);
                    continue;
                }

                var house = new House
                {
                    Rua = street,
                    TipoLogradouro = GetCell(row, streetTypeIndex).Trim(),
                    CEP = cep,
                    NumeroCasa = GetCell(row, numberIndex).Trim(),
                    Bairro = CoalesceAddressValue(importedNeighborhood, addressFallback?.Bairro),
                    Cidade = CoalesceAddressValue(importedCity, addressFallback?.Cidade),
                    Estado = CoalesceAddressValue(importedState, addressFallback?.Estado),
                    Pais = CoalesceAddressValue(importedCountry, addressFallback?.Pais),
                    Complemento = GetCell(row, complementIndex).Trim()
                };

                if (string.IsNullOrWhiteSpace(house.Pais))
                {
                    house.Pais = "Brasil";
                }

                house.PossuiComplemento = !string.IsNullOrWhiteSpace(house.Complemento);

                var key = GetHouseKey(house);
                if (housesByKey.TryGetValue(key, out var existingMatches) && existingMatches.Count > 1)
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: endereço corresponde a várias residências; revisão manual necessária.");
                    continue;
                }
                if (existingMatches?.SingleOrDefault() is { } existingHouse)
                {
                    if (!string.IsNullOrWhiteSpace(house.CEP)) existingHouse.CEP = house.CEP;
                    if (!string.IsNullOrWhiteSpace(house.Rua)) existingHouse.Rua = house.Rua;
                    if (!string.IsNullOrWhiteSpace(house.TipoLogradouro)) existingHouse.TipoLogradouro = house.TipoLogradouro;
                    if (!string.IsNullOrWhiteSpace(house.NumeroCasa)) existingHouse.NumeroCasa = house.NumeroCasa;
                    if (!string.IsNullOrWhiteSpace(house.Bairro)) existingHouse.Bairro = house.Bairro;
                    if (!string.IsNullOrWhiteSpace(house.Cidade)) existingHouse.Cidade = house.Cidade;
                    if (!string.IsNullOrWhiteSpace(house.Estado)) existingHouse.Estado = house.Estado;
                    if (!string.IsNullOrWhiteSpace(house.Pais)) existingHouse.Pais = house.Pais;
                    if (!string.IsNullOrWhiteSpace(house.Complemento)) existingHouse.Complemento = house.Complemento;
                    existingHouse.PossuiComplemento = !string.IsNullOrWhiteSpace(existingHouse.Complemento);

                    await houseService.UpdateHouseAsync(existingHouse);
                    result.UpdatedCount++;
                    result.MergedCount++;
                    result.Details.Add($"Linha {rowIndex + 1}: residência #{existingHouse.CasaId} mesclada: {existingHouse.Rua}, {existingHouse.NumeroCasa}, {existingHouse.Complemento}.");
                }
                else
                {
                    await houseService.SaveHouseAsync(house);
                    AddHouseKeys(housesByKey, house);
                    result.ImportedCount++;
                    result.Details.Add($"Linha {rowIndex + 1}: residência #{house.CasaId} criada.");
                }
            }
            catch (ArgumentException ex)
            {
                result.IgnoredCount++;
                result.Errors.Add($"Linha {rowIndex + 1}: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.IgnoredCount++;
                result.Errors.Add($"Linha {rowIndex + 1}: nao foi possivel importar a residencia. {ex.Message}");
            }

            processedProgressItems = ReportEvery(
                progress,
                processedProgressItems + 1,
                totalProgressItems,
                "Importando residencias",
                rowIndex);
        }

        Report(progress, totalProgressItems, totalProgressItems, "Concluindo");
        return result;
    }

    private static Dictionary<string, List<House>> BuildHouseKeyIndex(IEnumerable<House> houses)
    {
        var index = new Dictionary<string, List<House>>(StringComparer.OrdinalIgnoreCase);
        foreach (var house in houses)
        {
            AddHouseKeys(index, house);
        }

        return index;
    }

    private static void AddHouseKeys(Dictionary<string, List<House>> index, House house)
    {
        foreach (var key in GetHouseKeys(house))
        {
            if (!index.TryGetValue(key, out var matches)) index[key] = matches = [];
            if (matches.All(h => h.CasaId != house.CasaId)) matches.Add(house);
        }
    }

    private static IReadOnlyList<string> GetHouseKeys(House house)
    {
        return GetAddressStreetVariants(house.TipoLogradouro, house.Rua)
            .Select(street => $"{Normalize(house.CEP)}|{Normalize(house.Cidade)}|{Normalize(house.Estado)}|{Normalize(street)}|{Normalize(house.NumeroCasa)}|{Normalize(house.Complemento)}")
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetHouseKey(House house)
    {
        return GetHouseKeys(house).FirstOrDefault() ?? string.Empty;
    }

    private static IEnumerable<string> GetAddressStreetVariants(string? streetType, string? street)
    {
        var normalizedStreetType = streetType?.Trim() ?? string.Empty;
        var normalizedStreet = street?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(normalizedStreetType) && !string.IsNullOrWhiteSpace(normalizedStreet))
        {
            yield return $"{normalizedStreetType} {normalizedStreet}";
        }

        if (!string.IsNullOrWhiteSpace(normalizedStreet))
        {
            yield return normalizedStreet;
        }
    }

    private static (int RowIndex, Dictionary<string, int> HeaderMap)? FindHeaderRow(
        IReadOnlyList<List<string>> rows,
        params string[] requiredColumns)
    {
        var rowsToScan = Math.Min(rows.Count, 20);

        for (var rowIndex = 0; rowIndex < rowsToScan; rowIndex++)
        {
            var headerMap = BuildHeaderMap(rows[rowIndex]);
            if (requiredColumns.Any(requiredColumn => FindColumnIndex(headerMap, requiredColumn) is not null))
            {
                return (rowIndex, headerMap);
            }
        }

        return null;
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

        if (headerMap.TryGetValue(ACS_View.Domain.ValueObjects.ImportColumnAliases.Normalize(columnName), out var exact)) return exact;
        foreach (var item in headerMap)
            if (ACS_View.Domain.ValueObjects.ImportColumnAliases.Matches(item.Key, columnName)) return item.Value;

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

    private async Task<House?> ResolveAddressFallbackAsync(
        string cep,
        Dictionary<string, House?> cepCache)
    {
        var normalizedCep = NormalizeCep(cep);
        if (string.IsNullOrWhiteSpace(normalizedCep))
        {
            return null;
        }

        if (cepCache.TryGetValue(normalizedCep, out var cachedAddress))
        {
            return cachedAddress;
        }

        try
        {
            var address = await cepService.GetAddressByCepAsync(normalizedCep);
            cepCache[normalizedCep] = HasAddressData(address) ? address : null;
            return cepCache[normalizedCep];
        }
        catch
        {
            cepCache[normalizedCep] = null;
            return null;
        }
    }

    private static bool HasAddressData(House? address)
    {
        return address != null &&
               (!string.IsNullOrWhiteSpace(address.Rua) ||
                !string.IsNullOrWhiteSpace(address.Bairro) ||
                !string.IsNullOrWhiteSpace(address.Cidade) ||
                !string.IsNullOrWhiteSpace(address.Estado));
    }

    private static string CoalesceAddressValue(string importedValue, string? fallbackValue)
    {
        return !string.IsNullOrWhiteSpace(importedValue)
            ? importedValue.Trim()
            : fallbackValue?.Trim() ?? string.Empty;
    }

    private static bool HasMissingAddressValue(params string[] values)
    {
        return values.Any(string.IsNullOrWhiteSpace);
    }

    private static string NormalizeCep(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : Regex.Replace(value, @"\D", string.Empty);
    }

    private static int ReportEvery(
        IProgress<ImportProgressDto>? progress,
        int processedItems,
        int totalItems,
        string currentStep,
        int rowNumber)
    {
        if (rowNumber % 5 == 0 || processedItems >= totalItems)
        {
            Report(progress, processedItems, totalItems, currentStep);
        }

        return processedItems;
    }

    private static void Report(
        IProgress<ImportProgressDto>? progress,
        int processedItems,
        int totalItems,
        string currentStep)
    {
        progress?.Report(new ImportProgressDto
        {
            ProcessedItems = Math.Clamp(processedItems, 0, totalItems),
            TotalItems = totalItems,
            CurrentStep = currentStep
        });
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

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
