using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.ValueObjects;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ACS_View.UseCases.Services
{
    internal class PatientImportService(
        IPatientService patientService,
        ICepService cepService,
        ISQLiteConditionsRepository conditionsRepository,
        IPatientBolsaFamiliaRepository bolsaFamiliaRepository,
        ISpreadsheetReader spreadsheetReader,
        PatientFamilyLinkResolver familyLinkResolver,
        IImportHistoryService historyService) : IPatientImportService
    {
        public async Task<PatientImportResultDto> ImportAsync(
            Stream fileStream,
            PatientImportColumnMapDto columnMap,
            IProgress<ImportProgressDto>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new PatientImportResultDto();
            var history = new ImportHistory { FileName = columnMap.SourceFileName, StartedAt = DateTimeOffset.Now.ToString("O"), Status = "Concluída" };
            try
            {
                return await ImportCoreAsync(fileStream, columnMap, result, progress, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                history.Status = "Cancelada (dados já gravados foram mantidos)";
                result.Errors.Add("Importação cancelada; consulte as linhas já processadas.");
                throw;
            }
            catch (Exception ex)
            {
                history.Status = "Interrompida (pode conter dados já gravados)";
                result.Errors.Add(ex.GetBaseException().Message);
                throw;
            }
            finally
            {
                history.FinishedAt = DateTimeOffset.Now.ToString("O");
                if (history.Status == "Concluída" && result.Errors.Count > 0) history.Status = "Concluída com pendências";
                history.ReportJson = System.Text.Json.JsonSerializer.Serialize(result);
                await historyService.SaveAsync(history);
            }
        }

        private async Task<PatientImportResultDto> ImportCoreAsync(Stream fileStream, PatientImportColumnMapDto columnMap,
            PatientImportResultDto result, IProgress<ImportProgressDto>? progress, CancellationToken cancellationToken)
        {
            Report(progress, 0, 1, "Lendo planilha");

            cancellationToken.ThrowIfCancellationRequested();
            var rows = spreadsheetReader.ReadWorksheetRows(fileStream);

            if (rows.Count == 0)
            {
                result.Errors.Add("A planilha nao possui linhas para importar.");
                return result;
            }

            var headerMatch = FindHeaderRow(rows, columnMap.NameColumn);
            if (headerMatch is null)
            {
                var detectedColumns = rows.Count > 0
                    ? string.Join(", ", rows[0].Where(value => !string.IsNullOrWhiteSpace(value)).Take(12))
                    : string.Empty;

                result.Errors.Add($"Nao encontrei a coluna \"{columnMap.NameColumn}\". Colunas lidas: {detectedColumns}");
                return result;
            }

            var headerMap = ImportColumnMapper.Map(rows, headerMatch.Value.RowIndex, columnMap, result.Errors, result.Details);
            var columns = ResolveColumns(headerMap, columnMap);
            var dataRowCount = Math.Max(0, rows.Count - headerMatch.Value.RowIndex - 1);
            var totalProgressItems = Math.Max(1, dataRowCount * 3 + 5);
            var processedProgressItems = 1;

            Report(progress, processedProgressItems, totalProgressItems, "Importando pacientes");

            var rowContexts = new List<PatientImportRowContext>();
            var cepCache = new Dictionary<string, House?>();
            var existingPatients = await patientService.GetAllPatients() ?? [];
            var patientsBySusForUpsert = existingPatients
                .SelectMany(patient => patient.SusNumbers.Select(sus => new { Sus = sus, Patient = patient }))
                .GroupBy(item => item.Sus)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Patient).DistinctBy(patient => patient.Id).ToList());
            var patientsByIdentityForUpsert = existingPatients
                .Select(patient => new
                {
                    Patient = patient,
                    HasKey = TryBuildPatientIdentityKey(patient.Name, patient.MotherName, patient.BirthDate, out var key),
                    Key = key
                })
                .Where(item => item.HasKey)
                .GroupBy(item => item.Key)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Patient).ToList());


            for (var rowIndex = headerMatch.Value.RowIndex + 1; rowIndex < rows.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = rows[rowIndex];
                if (row.All(string.IsNullOrWhiteSpace)) continue;
                var name = GetCell(row, columns.NameIndex!.Value);

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: nome do paciente vazio; linha não importada.");
                    processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Importando pacientes", rowIndex);
                    continue;
                }

                var susNumber = GetCell(row, columns.SusIndex);
                var susNumbers = SusNumberSet.Parse(susNumber);
                var motherName = GetCell(row, columns.MotherIndex).Trim();
                var fatherName = GetCell(row, columns.FatherIndex).Trim();
                var observation = GetCell(row, columns.ObservationIndex).Trim();
                var birthDate = ParseDate(GetCell(row, columns.BirthDateIndex));
                var importedSex = ParseSex(GetCell(row, columns.SexIndex));
                if (row.All(string.IsNullOrWhiteSpace)) continue;
                if (!string.IsNullOrWhiteSpace(GetCell(row, columns.BirthDateIndex)) && (birthDate is null || !HasValidBirthDate(birthDate.Value)))
                {
                    result.Errors.Add($"Linha {rowIndex + 1}: data de nascimento inválida: '{GetCell(row, columns.BirthDateIndex)}'.");
                    birthDate = null;
                }
                if (!string.IsNullOrWhiteSpace(GetCell(row, columns.SexIndex)) && importedSex is null)
                    result.Errors.Add($"Linha {rowIndex + 1}: sexo não reconhecido: '{GetCell(row, columns.SexIndex)}'.");

                var hasIdentity = TryBuildPatientIdentityKey(name, motherName, birthDate, out var identityKey);
                var candidates = new List<Patient>();
                foreach (var sus in susNumbers)
                    if (patientsBySusForUpsert.TryGetValue(sus, out var susMatches)) candidates.AddRange(susMatches);
                if (hasIdentity && patientsByIdentityForUpsert.TryGetValue(identityKey, out var identityMatches)) candidates.AddRange(identityMatches);
                candidates = candidates.DistinctBy(p => p.Id).ToList();
                if (candidates.Count > 1 || (susNumbers.Count == 0 && !hasIdentity))
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: {name}: identidade insuficiente ou corresponde a vários cadastros ({string.Join(", ", candidates.Select(p => p.Id))}); revisão manual necessária.");
                    continue;
                }
                var existingPatient = candidates.SingleOrDefault();
                var before = existingPatient == null ? null : System.Text.Json.JsonSerializer.Serialize(existingPatient);
                var patient = existingPatient ?? new Patient();
                var isExistingPatient = patient.Id > 0;
                var patientChanged = isExistingPatient
                    ? UpdateImportedPatientFields(patient, name, susNumber, motherName, fatherName, observation, importedSex, birthDate)
                    : SetImportedPatientFields(patient, name, susNumber, motherName, fatherName, observation, importedSex, birthDate);

                foreach (var condition in columns.ConditionColumnIndexes)
                {
                    var value = GetCell(row, condition.ColumnIndex);
                    if (!string.IsNullOrWhiteSpace(value) && ParseOptionalBoolean(value) is null)
                        result.Errors.Add($"Linha {rowIndex + 1}: {condition.ConditionName}: valor não reconhecido '{value}', preservado o dado anterior.");
                }
                var responsibleValue = GetCell(row, columns.IsFamilyResponsibleIndex);
                if (!string.IsNullOrWhiteSpace(responsibleValue) && ParseOptionalBoolean(responsibleValue) is null)
                    result.Errors.Add($"Linha {rowIndex + 1}: responsável familiar: valor não reconhecido '{responsibleValue}'.");
                if (ParseOptionalBoolean(responsibleValue) == false && patient.FamilyResponsiblePatientId == patient.Id && patient.Id > 0)
                    result.Errors.Add($"Linha {rowIndex + 1}: {name}: planilha informa que não é responsável familiar, mas o cadastro é responsável de uma família; vínculo preservado para revisão manual.");
                var responsibleSus = GetCell(row, columns.FamilyResponsibleSusIndex).Trim();
                if (!string.IsNullOrWhiteSpace(responsibleSus) && patient.FamilyResponsibleSus != responsibleSus)
                {
                    patient.FamilyResponsibleSus = responsibleSus;
                    patientChanged = true;
                }
                var importedConditions = columns.ConditionColumnIndexes
                    .Where(map => ParseBoolean(GetCell(row, map.ColumnIndex)))
                    .Select(map => map.ConditionName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var importedBolsaFamilia = ParseBoolean(GetCell(row, columns.BolsaFamiliaIndex)) ||
                                           importedConditions.Any(condition =>
                                               HealthConditionCatalog.GetKey(condition) == HealthConditionCatalog.BolsaFamilia);

                importedConditions = importedConditions
                    .Where(condition => HealthConditionCatalog.GetKey(condition) != HealthConditionCatalog.BolsaFamilia)
                    .ToList();

                try
                {
                    if (isExistingPatient)
                    {
                        if (patientChanged)
                        {
                            await patientService.UpdatePatient(patient);
                        }
                    }
                    else
                    {
                        await patientService.CreatePatient(patient);
                    }

                    if (isExistingPatient)
                    {
                        result.MergedCount++;
                        result.Details.Add($"Linha {rowIndex + 1}: {name} mesclado no cadastro #{patient.Id}. {DescribeChanges(before!, patient)}");
                    }
                    else result.Details.Add($"Linha {rowIndex + 1}: {name} criado como cadastro #{patient.Id}.");

                    if (before != null)
                    {
                        var previous = System.Text.Json.JsonSerializer.Deserialize<Patient>(before)!;
                        foreach (var oldSus in previous.SusNumbers)
                            if (patientsBySusForUpsert.TryGetValue(oldSus, out var oldSusMatches))
                                oldSusMatches.RemoveAll(p => p.Id == patient.Id);
                        if (TryBuildPatientIdentityKey(previous.Name, previous.MotherName, previous.BirthDate, out var oldIdentity) &&
                            patientsByIdentityForUpsert.TryGetValue(oldIdentity, out var oldIdentityMatches))
                            oldIdentityMatches.RemoveAll(p => p.Id == patient.Id);
                    }
                    foreach (var savedSus in patient.SusNumbers)
                    {
                        if (!patientsBySusForUpsert.TryGetValue(savedSus, out var matches)) patientsBySusForUpsert[savedSus] = matches = [];
                        matches.Add(patient);
                    }

                    if (TryBuildPatientIdentityKey(patient.Name, patient.MotherName, patient.BirthDate, out var savedIdentityKey))
                    {
                        if (!patientsByIdentityForUpsert.TryGetValue(savedIdentityKey, out var matches)) patientsByIdentityForUpsert[savedIdentityKey] = matches = [];
                        matches.Add(patient);
                    }

                    var explicitConditions = columns.ConditionColumnIndexes
                        .Where(map => ParseOptionalBoolean(GetCell(row, map.ColumnIndex)) is not null)
                        .Select(map => map.ConditionName).ToList();
                    var conditionsChanged = await SyncImportedConditionsAsync(patient.Id, explicitConditions, importedConditions);
                    if (conditionsChanged) result.Details.Add($"Linha {rowIndex + 1}: condições de saúde atualizadas: {string.Join(", ", explicitConditions)}.");
                    var bolsaFamiliaChanged = await AddMissingImportedBolsaFamiliaAsync(patient.Id, importedBolsaFamilia);

                    var importedStreet = GetCell(row, columns.PatientStreetIndex);
                    var importedNeighborhood = GetCell(row, columns.PatientNeighborhoodIndex);
                    var importedCity = GetCell(row, columns.PatientCityIndex);
                    var importedState = GetCell(row, columns.PatientStateIndex);
                    var needsCepFallback = HasMissingAddressValue(importedStreet, importedNeighborhood, importedCity, importedState);
                    var addressFallback = needsCepFallback
                        ? await ResolveAddressFallbackAsync(GetCell(row, columns.PatientCepIndex), cepCache)
                        : null;

                    var street = CoalesceAddressValue(importedStreet, addressFallback?.Rua);
                    var neighborhood = CoalesceAddressValue(importedNeighborhood, addressFallback?.Bairro);
                    var city = CoalesceAddressValue(importedCity, addressFallback?.Cidade);
                    var state = CoalesceAddressValue(importedState, addressFallback?.Estado);

                    rowContexts.Add(new PatientImportRowContext
                    {
                        RowNumber = rowIndex + 1,
                        Patient = patient,
                        MotherName = patient.MotherName,
                        FatherName = patient.FatherName,
                        FamilyResponsibleSus = GetCell(row, columns.FamilyResponsibleSusIndex).Trim(),
                        IsFamilyResponsible = ParseBoolean(GetCell(row, columns.IsFamilyResponsibleIndex)),
                        ImportedHouse = new House { CEP = GetCell(row, columns.PatientCepIndex), Rua = street,
                            NumeroCasa = GetCell(row, columns.PatientHouseNumberIndex), Complemento = GetCell(row, columns.PatientComplementIndex),
                            Bairro = neighborhood, Cidade = city, Estado = state },
                        AddressKeys = BuildAddressKeys(
                            GetCell(row, columns.PatientCepIndex),
                            GetCell(row, columns.PatientStreetTypeIndex),
                            street,
                            GetCell(row, columns.PatientHouseNumberIndex),
                            GetCell(row, columns.PatientComplementIndex),
                            neighborhood,
                            city,
                            state)
                    });

                    if (isExistingPatient)
                    {
                        if (patientChanged || conditionsChanged || bolsaFamiliaChanged)
                        {
                            result.UpdatedCount++;
                        }
                        else
                        {
                            result.IgnoredCount++;
                        }
                    }
                    else
                    {
                        result.ImportedCount++;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (ArgumentException ex)
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: nao foi possivel importar o paciente. {ex.Message}");
                }

                processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Importando pacientes", rowIndex);
            }

            if (columnMap.EnableAutomaticFamilyLinking && rowContexts.Count > 0)
            {
                processedProgressItems = await familyLinkResolver.ResolveAsync(
                    rowContexts,
                    columnMap,
                    result,
                    progress,
                    processedProgressItems,
                    totalProgressItems,
                    cancellationToken);
            }

            Report(progress, totalProgressItems, totalProgressItems, "Concluindo");
            return result;
        }

        private static bool SetImportedPatientFields(
            Patient patient,
            string name,
            string susNumber,
            string motherName,
            string fatherName,
            string observation,
            string? importedSex,
            DateTime? birthDate)
        {
            patient.Name = name.Trim();
            patient.SusNumber = susNumber.Trim();
            patient.MotherName = motherName.Trim();
            patient.FatherName = fatherName.Trim();
            patient.Observacao = observation.Trim();

            if (!string.IsNullOrWhiteSpace(importedSex))
            {
                patient.Sexo = importedSex;
            }

            if (birthDate is not null)
            {
                patient.BirthDate = birthDate.Value;
            }

            return true;
        }

        private static string DescribeChanges(string beforeJson, Patient patient)
        {
            var before = System.Text.Json.JsonSerializer.Deserialize<Patient>(beforeJson)!;
            var fields = new (string Label, string Old, string New)[]
            {
                ("Nome", before.Name, patient.Name), ("CNS", before.SusNumber, patient.SusNumber),
                ("Mãe", before.MotherName, patient.MotherName), ("Pai", before.FatherName, patient.FatherName),
                ("Nascimento", before.BirthDate.ToString("dd/MM/yyyy"), patient.BirthDate.ToString("dd/MM/yyyy")),
                ("Sexo", before.Sexo, patient.Sexo), ("Observação", before.Observacao, patient.Observacao),
                ("CNS do responsável", before.FamilyResponsibleSus ?? "", patient.FamilyResponsibleSus ?? "")
            };
            var changes = fields.Where(f => f.Old != f.New).Select(f => $"{f.Label}: '{f.Old}' → '{f.New}'").ToList();
            return changes.Count == 0 ? "Dados pessoais mantidos; condições e vínculos conferidos." : string.Join("; ", changes);
        }

        private static bool UpdateImportedPatientFields(Patient patient, string name, string susNumber, string motherName,
            string fatherName, string observation, string? importedSex, DateTime? birthDate)
        {
            var before = System.Text.Json.JsonSerializer.Serialize(patient);
            if (!string.IsNullOrWhiteSpace(name)) patient.Name = name.Trim();
            patient.AddSusNumbers(susNumber);
            if (!string.IsNullOrWhiteSpace(motherName)) patient.MotherName = motherName.Trim();
            if (!string.IsNullOrWhiteSpace(fatherName)) patient.FatherName = fatherName.Trim();
            if (!string.IsNullOrWhiteSpace(observation) && !patient.Observacao.Split(" | ").Contains(observation.Trim()))
                patient.Observacao = string.IsNullOrWhiteSpace(patient.Observacao) ? observation.Trim() : patient.Observacao + " | " + observation.Trim();
            if (importedSex != null) patient.Sexo = importedSex;
            if (birthDate != null) patient.BirthDate = birthDate.Value;
            return before != System.Text.Json.JsonSerializer.Serialize(patient);
        }

        private static ImportColumns ResolveColumns(
            IReadOnlyDictionary<string, int> headerMap,
            PatientImportColumnMapDto columnMap)
        {
            return new ImportColumns
            {
                NameIndex = FindColumnIndex(headerMap, columnMap.NameColumn),
                SusIndex = FindColumnIndex(headerMap, columnMap.SusNumberColumn),
                MotherIndex = FindColumnIndex(headerMap, columnMap.MotherNameColumn),
                FatherIndex = FindColumnIndex(headerMap, columnMap.FatherNameColumn),
                SexIndex = FindColumnIndex(headerMap, columnMap.SexColumn),
                BirthDateIndex = FindColumnIndex(headerMap, columnMap.BirthDateColumn),
                ObservationIndex = FindColumnIndex(headerMap, columnMap.ObservationColumn),
                BolsaFamiliaIndex = FindColumnIndex(headerMap, columnMap.BolsaFamiliaColumn),
                PatientCepIndex = FindColumnIndex(headerMap, columnMap.PatientCepColumn),
                PatientStreetTypeIndex = FindColumnIndex(headerMap, columnMap.PatientStreetTypeColumn),
                PatientStreetIndex = FindColumnIndex(headerMap, columnMap.PatientStreetColumn),
                PatientHouseNumberIndex = FindColumnIndex(headerMap, columnMap.PatientHouseNumberColumn),
                PatientNeighborhoodIndex = FindColumnIndex(headerMap, columnMap.PatientNeighborhoodColumn),
                PatientCityIndex = FindColumnIndex(headerMap, columnMap.PatientCityColumn),
                PatientStateIndex = FindColumnIndex(headerMap, columnMap.PatientStateColumn),
                PatientComplementIndex = FindColumnIndex(headerMap, columnMap.PatientComplementColumn),
                IsFamilyResponsibleIndex = FindColumnIndex(headerMap, columnMap.IsFamilyResponsibleColumn),
                FamilyResponsibleSusIndex = FindColumnIndex(headerMap, columnMap.FamilyResponsibleSusColumn),
                ConditionColumnIndexes = columnMap.HealthConditionColumns
                    .Where(map => !string.IsNullOrWhiteSpace(map.ConditionName) && !string.IsNullOrWhiteSpace(map.ColumnName))
                    .Select(map => new ImportConditionColumn(map.ConditionName, FindColumnIndex(headerMap, map.ColumnName)))
                    .Where(map => map.ColumnIndex is not null)
                    .ToList()
            };
        }

        private async Task<bool> SyncImportedConditionsAsync(
            int patientId,
            IEnumerable<string> mappedConditionNames,
            IEnumerable<string> selectedConditionNames)
        {
            var mappedKeys = mappedConditionNames
                .Select(HealthConditionCatalog.GetKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (mappedKeys.Count == 0)
            {
                return false;
            }

            var changed = false;
            var currentConditions = await conditionsRepository.GetConditionsByPatientIdAsync(patientId);
            var selectedKeys = selectedConditionNames.Select(HealthConditionCatalog.GetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var condition in currentConditions.Where(condition => mappedKeys.Contains(HealthConditionCatalog.GetKey(condition.Description)) && !selectedKeys.Contains(HealthConditionCatalog.GetKey(condition.Description))))
            {
                await conditionsRepository.DeleteConditionAsync(condition.Id);
                changed = true;
            }

            foreach (var conditionName in selectedConditionNames)
            {
                if (currentConditions.Any(c => HealthConditionCatalog.GetKey(c.Description) == HealthConditionCatalog.GetKey(conditionName))) continue;
                await conditionsRepository.InsertConditionAsync(new PatientConditions
                {
                    PatientId = patientId,
                    Description = conditionName
                });
                changed = true;
            }

            return changed;
        }

        private async Task<bool> AddMissingImportedBolsaFamiliaAsync(int patientId, bool isBeneficiary)
        {
            if (!isBeneficiary)
            {
                return false;
            }

            if (await bolsaFamiliaRepository.GetByPatientIdAsync(patientId) is not null)
            {
                return false;
            }

            await bolsaFamiliaRepository.UpsertAsync(new PatientBolsaFamilia
            {
                PatientId = patientId,
                ResponsiblePatientId = patientId,
                NisNumber = string.Empty
            });
            return true;
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

            if (headerMap.TryGetValue(ImportColumnAliases.Normalize(columnName), out var exact)) return exact;
            if (headerMap.TryGetValue(Normalize(columnName), out exact)) return exact;
            foreach (var item in headerMap)
                if (ImportColumnAliases.Matches(item.Key, columnName)) return item.Value;

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

        private static bool? ParseOptionalBoolean(string value)
        {
            var key = ImportColumnAliases.Normalize(value);
            if (key is "1" or "sim" or "s" or "true" or "verdadeiro" or "x") return true;
            if (key is "0" or "nao" or "n" or "false" or "falso") return false;
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


        private static string? ParseSex(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = Normalize(value);
            var compact = Compact(normalized);
            return compact switch
            {
                "0" or "m" or "masc" or "masculino" => nameof(Sexo.Masculino),
                "1" or "f" or "fem" or "feminino" => nameof(Sexo.Feminino),
                "2" or "i" or "ind" or "indeterminado" or "ignorado" or "naoinformado" => nameof(Sexo.Indeterminado),
                _ => null
            };
        }

        private async Task<House?> ResolveAddressFallbackAsync(
            string cep,
            Dictionary<string, House?> cepCache)
        {
            var normalizedCep = NormalizeSus(cep);
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
                cepCache[normalizedCep] = address;
                return address;
            }
            catch
            {
                cepCache[normalizedCep] = null;
                return null;
            }
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

        private static string? BuildAddressKey(
            string cep,
            string streetType,
            string street,
            string number,
            string complement,
            string neighborhood,
            string city,
            string state)
        {
            return BuildAddressKeys(cep, streetType, street, number, complement, neighborhood, city, state).FirstOrDefault();
        }

        private static IReadOnlyList<string> BuildAddressKeys(
            string cep,
            string streetType,
            string street,
            string number,
            string complement,
            string neighborhood,
            string city,
            string state)
        {
            var normalizedCep = NormalizeSus(cep);
            var normalizedNumber = Compact(Normalize(number));
            var normalizedComplement = Normalize(complement);
            var normalizedNeighborhood = Normalize(neighborhood);
            var normalizedCity = Normalize(city);
            var normalizedState = Normalize(state).ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(normalizedNumber))
            {
                return [];
            }

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var streetVariant in GetStreetVariants(streetType, street))
            {
                var normalizedStreet = Normalize(streetVariant);
                AddAddressKey(keys, normalizedCep, normalizedStreet, normalizedNumber, normalizedComplement, normalizedNeighborhood, normalizedCity, normalizedState);
                AddAddressKey(keys, string.Empty, normalizedStreet, normalizedNumber, normalizedComplement, normalizedNeighborhood, normalizedCity, normalizedState);
                AddAddressKey(keys, normalizedCep, normalizedStreet, normalizedNumber, normalizedComplement, string.Empty, string.Empty, string.Empty);
                AddAddressKey(keys, string.Empty, normalizedStreet, normalizedNumber, normalizedComplement, string.Empty, string.Empty, string.Empty);
            }

            return keys.ToList();
        }

        private static void AddAddressKey(
            ISet<string> keys,
            string cep,
            string street,
            string number,
            string complement,
            string neighborhood,
            string city,
            string state)
        {
            if (!string.IsNullOrWhiteSpace(street) && !string.IsNullOrWhiteSpace(number))
            {
                keys.Add(string.Join("|", cep, street, number, complement, neighborhood, city, state));
            }
        }

        private static IEnumerable<string> GetStreetVariants(string? streetType, string? street)
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

        private static string NormalizeSus(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : Regex.Replace(value, @"\D", string.Empty);
        }

        private static bool TryBuildPatientIdentityKey(
            string? name,
            string? motherName,
            DateTime? birthDate,
            out PatientIdentityKey key)
        {
            key = default;

            if (birthDate is null || !HasValidBirthDate(birthDate.Value))
            {
                return false;
            }

            var normalizedName = Normalize(name ?? string.Empty);
            var normalizedMotherName = Normalize(motherName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedMotherName))
            {
                return false;
            }

            key = new PatientIdentityKey(normalizedName, normalizedMotherName, birthDate.Value.Date);
            return true;
        }

        private static bool HasValidBirthDate(DateTime date)
        {
            return date.Year >= 1900 && date.Date <= DateTime.Today;
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

        private sealed class ImportColumns
        {
            public int? NameIndex { get; init; }
            public int? SusIndex { get; init; }
            public int? MotherIndex { get; init; }
            public int? FatherIndex { get; init; }
            public int? SexIndex { get; init; }
            public int? BirthDateIndex { get; init; }
            public int? ObservationIndex { get; init; }
            public int? BolsaFamiliaIndex { get; init; }
            public int? PatientCepIndex { get; init; }
            public int? PatientStreetTypeIndex { get; init; }
            public int? PatientStreetIndex { get; init; }
            public int? PatientHouseNumberIndex { get; init; }
            public int? PatientNeighborhoodIndex { get; init; }
            public int? PatientCityIndex { get; init; }
            public int? PatientStateIndex { get; init; }
            public int? PatientComplementIndex { get; init; }
            public int? IsFamilyResponsibleIndex { get; init; }
            public int? FamilyResponsibleSusIndex { get; init; }
            public List<ImportConditionColumn> ConditionColumnIndexes { get; init; } = [];
        }

        private sealed record ImportConditionColumn(string ConditionName, int? ColumnIndex);

        private readonly record struct PatientIdentityKey(string Name, string MotherName, DateTime BirthDate);

    }
}
