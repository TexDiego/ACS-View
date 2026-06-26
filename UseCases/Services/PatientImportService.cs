using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.ValueObjects;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ACS_View.UseCases.Services
{
    internal class PatientImportService(
        IPatientService patientService,
        IHouseService houseService,
        IFamilyService familyService,
        ICepService cepService,
        ISQLiteConditionsRepository conditionsRepository) : IPatientImportService
    {
        private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        public async Task<PatientImportResultDto> ImportAsync(
            Stream fileStream,
            PatientImportColumnMapDto columnMap,
            IProgress<ImportProgressDto>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new PatientImportResultDto();
            Report(progress, 0, 1, "Lendo planilha");

            cancellationToken.ThrowIfCancellationRequested();
            var rows = ReadWorksheetRows(fileStream);

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

            var headerMap = headerMatch.Value.HeaderMap;
            var columns = ResolveColumns(headerMap, columnMap);
            var dataRowCount = Math.Max(0, rows.Count - headerMatch.Value.RowIndex - 1);
            var totalProgressItems = Math.Max(1, dataRowCount * 3 + 5);
            var processedProgressItems = 1;

            Report(progress, processedProgressItems, totalProgressItems, "Importando pacientes");

            var rowContexts = new List<PatientImportRowContext>();
            var cepCache = new Dictionary<string, House?>();
            var existingPatients = await patientService.GetAllPatients() ?? [];
            var patientsBySusForUpsert = existingPatients
                .Where(patient => !string.IsNullOrWhiteSpace(patient.SusNumber))
                .GroupBy(patient => NormalizeSus(patient.SusNumber))
                .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                .ToDictionary(group => group.Key, group => group.First());
            var patientsByIdentityForUpsert = existingPatients
                .Select(patient => new
                {
                    Patient = patient,
                    HasKey = TryBuildPatientIdentityKey(patient.Name, patient.MotherName, patient.BirthDate, out var key),
                    Key = key
                })
                .Where(item => item.HasKey)
                .GroupBy(item => item.Key)
                .ToDictionary(group => group.Key, group => group.First().Patient);
            var importedPatientKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var rowIndex = headerMatch.Value.RowIndex + 1; rowIndex < rows.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = rows[rowIndex];
                var name = GetCell(row, columns.NameIndex!.Value);

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.IgnoredCount++;
                    processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Importando pacientes", rowIndex);
                    continue;
                }

                var susNumber = GetCell(row, columns.SusIndex);
                var normalizedSus = NormalizeSus(susNumber);
                var motherName = GetCell(row, columns.MotherIndex).Trim();
                var birthDate = ParseDate(GetCell(row, columns.BirthDateIndex));
                Patient? existingPatient = null;
                PatientIdentityKey identityKey = default;
                string? importKey;

                if (!string.IsNullOrWhiteSpace(normalizedSus))
                {
                    patientsBySusForUpsert.TryGetValue(normalizedSus, out existingPatient);
                    importKey = BuildSusImportKey(normalizedSus);
                }
                else if (TryBuildPatientIdentityKey(name, motherName, birthDate, out identityKey))
                {
                    patientsByIdentityForUpsert.TryGetValue(identityKey, out existingPatient);
                    importKey = BuildIdentityImportKey(identityKey);
                }
                else
                {
                    result.IgnoredCount++;
                    result.Errors.Add($"Linha {rowIndex + 1}: paciente sem SUS precisa ter nome, nome da mae e data de nascimento valida para evitar duplicidade.");
                    processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Importando pacientes", rowIndex);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(importKey) && importedPatientKeys.Contains(importKey))
                {
                    result.IgnoredCount++;
                    processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Importando pacientes", rowIndex);
                    continue;
                }

                var patient = existingPatient ?? new Patient();

                patient.Name = name.Trim();
                if (!string.IsNullOrWhiteSpace(susNumber) || patient.Id == 0)
                {
                    patient.SusNumber = susNumber.Trim();
                }

                patient.MotherName = motherName;
                patient.FatherName = GetCell(row, columns.FatherIndex).Trim();
                patient.Observacao = GetCell(row, columns.ObservationIndex).Trim();

                var importedSex = ParseSex(GetCell(row, columns.SexIndex));
                if (!string.IsNullOrWhiteSpace(importedSex))
                {
                    patient.Sexo = importedSex;
                }

                var importedConditions = columns.ConditionColumnIndexes
                    .Where(map => ParseBoolean(GetCell(row, map.ColumnIndex)))
                    .Select(map => map.ConditionName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                patient.BolsaFamilia = ParseBoolean(GetCell(row, columns.BolsaFamiliaIndex)) ||
                                       importedConditions.Any(condition =>
                                           HealthConditionCatalog.GetKey(condition) == HealthConditionCatalog.BolsaFamilia);

                if (birthDate is not null)
                {
                    patient.BirthDate = birthDate.Value;
                }

                var isExistingPatient = patient.Id > 0;

                try
                {
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
                        var savedSus = NormalizeSus(patient.SusNumber);
                        patientsBySusForUpsert[savedSus] = patient;
                        importedPatientKeys.Add(BuildSusImportKey(savedSus));
                    }

                    if (TryBuildPatientIdentityKey(patient.Name, patient.MotherName, patient.BirthDate, out var savedIdentityKey))
                    {
                        patientsByIdentityForUpsert[savedIdentityKey] = patient;
                        importedPatientKeys.Add(BuildIdentityImportKey(savedIdentityKey));
                    }

                    if (!string.IsNullOrWhiteSpace(importKey))
                    {
                        importedPatientKeys.Add(importKey);
                    }

                    await SyncImportedConditionsAsync(patient.Id, columns.ConditionColumnIndexes.Select(map => map.ConditionName), importedConditions);

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
                    result.Errors.Add($"Linha {rowIndex + 1}: nao foi possivel importar o paciente. {ex.Message}");
                }

                processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Importando pacientes", rowIndex);
            }

            if (columnMap.EnableAutomaticFamilyLinking && rowContexts.Count > 0)
            {
                processedProgressItems = await ResolveFamilyLinksAsync(
                    rowContexts,
                    columnMap,
                    progress,
                    processedProgressItems,
                    totalProgressItems,
                    cancellationToken);
            }

            Report(progress, totalProgressItems, totalProgressItems, "Concluindo");
            return result;
        }

        private async Task<int> ResolveFamilyLinksAsync(
            List<PatientImportRowContext> rowContexts,
            PatientImportColumnMapDto columnMap,
            IProgress<ImportProgressDto>? progress,
            int processedProgressItems,
            int totalProgressItems,
            CancellationToken cancellationToken)
        {
            Report(progress, processedProgressItems, totalProgressItems, "Resolvendo residencias");

            var allPatients = await patientService.GetAllPatients() ?? [];
            var patientsById = allPatients.ToDictionary(patient => patient.Id);
            foreach (var context in rowContexts)
            {
                if (patientsById.TryGetValue(context.Patient.Id, out var reloadedPatient))
                {
                    context.Patient = reloadedPatient;
                }
            }

            var houses = await houseService.GetAllHousesAsync();
            var housesByAddressKey = houses
                .SelectMany(house => BuildAddressKeys(house.CEP, house.TipoLogradouro, house.Rua, house.NumeroCasa, house.Complemento, house.Bairro, house.Cidade, house.Estado)
                    .Select(key => new
                    {
                        Key = key,
                        House = house
                    }))
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .GroupBy(item => item.Key)
                .ToDictionary(group => group.Key, group => group.Select(item => item.House).ToList());

            foreach (var context in rowContexts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ResolveUniqueHouse(context.AddressKeys, housesByAddressKey) is { } matchedHouse &&
                    ShouldSetLink(context.Patient.HouseId, columnMap.OverwriteExistingFamilyLinks))
                {
                    context.Patient.HouseId = matchedHouse.CasaId;
                    context.ResolvedHouseId = matchedHouse.CasaId;
                    await patientService.UpdatePatient(context.Patient);
                }
                else if (context.Patient.HouseId > 0)
                {
                    context.ResolvedHouseId = context.Patient.HouseId;
                }

                processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Resolvendo residencias", context.RowNumber);
            }

            Report(progress, processedProgressItems, totalProgressItems, "Resolvendo vinculos familiares");
            var nextFamilyIdByHouse = new Dictionary<int, int>();
            var importedResponsibleContexts = rowContexts
                .Where(context => context.IsFamilyResponsible && context.Patient.HouseId > 0)
                .ToList();

            foreach (var context in importedResponsibleContexts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await EnsureResponsibleFamilyAsync(context.Patient, columnMap, nextFamilyIdByHouse);
                processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Resolvendo vinculos familiares", context.RowNumber);
            }

            var importedResponsiblesBySus = rowContexts
                .Where(context => context.Patient.HouseId > 0 && !string.IsNullOrWhiteSpace(context.Patient.SusNumber))
                .GroupBy(context => NormalizeSus(context.Patient.SusNumber))
                .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                .ToDictionary(group => group.Key, group => group.Select(context => context.Patient).DistinctBy(patient => patient.Id).ToList());

            var indexes = BuildPatientIndexes(allPatients, rowContexts);
            foreach (var context in rowContexts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var changed = await ResolveResponsibleAsync(
                    context,
                    columnMap,
                    importedResponsiblesBySus,
                    indexes,
                    nextFamilyIdByHouse);

                if (changed)
                {
                    await patientService.UpdatePatient(context.Patient);
                }

                processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Resolvendo vinculos familiares", context.RowNumber);
            }

            indexes = BuildPatientIndexes(allPatients, rowContexts);
            var contextsByPatientId = rowContexts.ToDictionary(context => context.Patient.Id);

            Report(progress, processedProgressItems, totalProgressItems, "Resolvendo mae e pai");
            foreach (var context in rowContexts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var changed = false;

                if (ShouldSetNullableLink(context.Patient.MotherPatientId, columnMap.OverwriteExistingFamilyLinks))
                {
                    var mother = ResolveParent(context.Patient, context.MotherName, ParentKind.Mother, indexes, contextsByPatientId, columnMap);
                    if (mother != null && context.Patient.MotherPatientId != mother.Id)
                    {
                        context.Patient.MotherPatientId = mother.Id;
                        changed = true;
                    }
                }

                if (ShouldSetNullableLink(context.Patient.FatherPatientId, columnMap.OverwriteExistingFamilyLinks))
                {
                    var father = ResolveParent(context.Patient, context.FatherName, ParentKind.Father, indexes, contextsByPatientId, columnMap);
                    if (father != null && context.Patient.FatherPatientId != father.Id)
                    {
                        context.Patient.FatherPatientId = father.Id;
                        changed = true;
                    }
                }

                if (changed)
                {
                    await patientService.UpdatePatient(context.Patient);
                }

                processedProgressItems = ReportEvery(progress, processedProgressItems + 1, totalProgressItems, "Resolvendo mae e pai", context.RowNumber);
            }

            return processedProgressItems;
        }

        private async Task EnsureResponsibleFamilyAsync(
            Patient responsible,
            PatientImportColumnMapDto columnMap,
            Dictionary<int, int> nextFamilyIdByHouse)
        {
            var changed = false;

            if (ShouldSetNullableLink(responsible.FamilyResponsiblePatientId, columnMap.OverwriteExistingFamilyLinks))
            {
                responsible.FamilyResponsiblePatientId = responsible.Id;
                responsible.FamilyResponsibleSus = responsible.SusNumber;
                changed = true;
            }

            if (responsible.HouseId > 0 && ShouldSetLink(responsible.FamilyId, columnMap.OverwriteExistingFamilyLinks))
            {
                responsible.FamilyId = await GetNextFamilyIdAsync(responsible.HouseId, nextFamilyIdByHouse);
                changed = true;
            }

            if (changed)
            {
                await patientService.UpdatePatient(responsible);
            }
        }

        private async Task<bool> ResolveResponsibleAsync(
            PatientImportRowContext context,
            PatientImportColumnMapDto columnMap,
            IReadOnlyDictionary<string, List<Patient>> importedResponsiblesBySus,
            PatientImportIndexes indexes,
            Dictionary<int, int> nextFamilyIdByHouse)
        {
            if (string.IsNullOrWhiteSpace(context.FamilyResponsibleSus) ||
                context.Patient.HouseId <= 0)
            {
                return false;
            }

            var normalizedSus = NormalizeSus(context.FamilyResponsibleSus);
            if (string.IsNullOrWhiteSpace(normalizedSus))
            {
                return false;
            }

            var candidates = importedResponsiblesBySus.TryGetValue(normalizedSus, out var importedCandidates)
                ? importedCandidates
                : [];

            candidates = candidates
                .Where(candidate => candidate.HouseId == context.Patient.HouseId)
                .DistinctBy(candidate => candidate.Id)
                .ToList();

            if (candidates.Count == 0 && columnMap.AllowGlobalUniqueResponsibleMatch &&
                indexes.PatientsBySus.TryGetValue(normalizedSus, out var globalCandidates))
            {
                candidates = globalCandidates
                    .Where(candidate => candidate.HouseId == context.Patient.HouseId)
                    .DistinctBy(candidate => candidate.Id)
                    .ToList();
            }

            if (candidates.Count != 1)
            {
                return false;
            }

            var responsible = candidates[0];
            var changed = false;

            if (responsible.FamilyId <= 0)
            {
                responsible.FamilyId = await GetNextFamilyIdAsync(responsible.HouseId, nextFamilyIdByHouse);
                responsible.FamilyResponsiblePatientId = responsible.Id;
                responsible.FamilyResponsibleSus = responsible.SusNumber;
                await patientService.UpdatePatient(responsible);
            }

            if (ShouldSetNullableLink(context.Patient.FamilyResponsiblePatientId, columnMap.OverwriteExistingFamilyLinks) &&
                context.Patient.FamilyResponsiblePatientId != responsible.Id)
            {
                context.Patient.FamilyResponsiblePatientId = responsible.Id;
                context.Patient.FamilyResponsibleSus = responsible.SusNumber;
                changed = true;
            }

            if (responsible.FamilyId > 0 && ShouldSetLink(context.Patient.FamilyId, columnMap.OverwriteExistingFamilyLinks))
            {
                context.Patient.FamilyId = responsible.FamilyId;
                changed = true;
            }

            return changed;
        }

        private static House? ResolveUniqueHouse(
            IReadOnlyList<string> addressKeys,
            IReadOnlyDictionary<string, List<House>> housesByAddressKey)
        {
            foreach (var key in addressKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!housesByAddressKey.TryGetValue(key, out var houses))
                {
                    continue;
                }

                var candidates = houses.DistinctBy(house => house.CasaId).ToList();
                return candidates.Count == 1 ? candidates[0] : null;
            }

            return null;
        }

        private async Task<int> GetNextFamilyIdAsync(int houseId, Dictionary<int, int> nextFamilyIdByHouse)
        {
            if (!nextFamilyIdByHouse.TryGetValue(houseId, out var nextFamilyId))
            {
                nextFamilyId = await familyService.GetMaxIdAsync(houseId) + 1;
            }

            nextFamilyIdByHouse[houseId] = nextFamilyId + 1;
            return nextFamilyId;
        }

        private static Patient? ResolveParent(
            Patient child,
            string parentName,
            ParentKind parentKind,
            PatientImportIndexes indexes,
            IReadOnlyDictionary<int, PatientImportRowContext> contextsByPatientId,
            PatientImportColumnMapDto columnMap)
        {
            var normalizedName = Normalize(parentName);
            if (Compact(normalizedName).Length < 5)
            {
                return null;
            }

            if (child.HouseId > 0 &&
                indexes.PatientsByNameAndHouse.TryGetValue((normalizedName, child.HouseId), out var sameHouseCandidates))
            {
                var match = SinglePlausibleParent(child, sameHouseCandidates, parentKind, columnMap);
                if (match != null)
                {
                    return match;
                }
            }

            if (contextsByPatientId.TryGetValue(child.Id, out var childContext))
            {
                foreach (var addressKey in childContext.AddressKeys)
                {
                    if (indexes.PatientsByNameAndAddress.TryGetValue((normalizedName, addressKey), out var sameAddressCandidates))
                    {
                        var match = SinglePlausibleParent(child, sameAddressCandidates, parentKind, columnMap);
                        if (match != null)
                        {
                            return match;
                        }
                    }
                }
            }

            if (columnMap.AllowGlobalUniqueParentMatch &&
                indexes.PatientsByName.TryGetValue(normalizedName, out var globalCandidates))
            {
                return SinglePlausibleParent(child, globalCandidates, parentKind, columnMap);
            }

            return null;
        }

        private static Patient? SinglePlausibleParent(
            Patient child,
            IEnumerable<Patient> candidates,
            ParentKind parentKind,
            PatientImportColumnMapDto columnMap)
        {
            var plausibleCandidates = candidates
                .Where(candidate => candidate.Id != child.Id)
                .Where(candidate => HasPlausibleAgeDifference(child, candidate, parentKind, columnMap))
                .DistinctBy(candidate => candidate.Id)
                .ToList();

            return plausibleCandidates.Count == 1 ? plausibleCandidates[0] : null;
        }

        private static bool HasPlausibleAgeDifference(
            Patient child,
            Patient parent,
            ParentKind parentKind,
            PatientImportColumnMapDto columnMap)
        {
            if (!HasValidBirthDate(child.BirthDate) || !HasValidBirthDate(parent.BirthDate))
            {
                return false;
            }

            var difference = child.BirthDate.Year - parent.BirthDate.Year;
            if (parent.BirthDate.Date > child.BirthDate.Date.AddYears(-difference))
            {
                difference--;
            }

            var maximumDifference = parentKind == ParentKind.Mother
                ? columnMap.MaximumMotherAgeDifferenceYears
                : columnMap.MaximumFatherAgeDifferenceYears;

            return difference >= columnMap.MinimumParentAgeDifferenceYears &&
                   difference <= maximumDifference;
        }

        private static bool HasValidBirthDate(DateTime date)
        {
            return date.Year > 1900 && date.Date < DateTime.Today;
        }

        private static bool ShouldSetLink(int currentValue, bool overwrite)
        {
            return overwrite || currentValue <= 0;
        }

        private static bool ShouldSetNullableLink(int? currentValue, bool overwrite)
        {
            return overwrite || currentValue is null or <= 0;
        }

        private static PatientImportIndexes BuildPatientIndexes(
            IEnumerable<Patient> patients,
            IEnumerable<PatientImportRowContext> rowContexts)
        {
            var patientList = patients.ToList();
            var contextList = rowContexts.ToList();
            return new PatientImportIndexes
            {
                PatientsBySus = patientList
                    .Where(patient => !string.IsNullOrWhiteSpace(patient.SusNumber))
                    .GroupBy(patient => NormalizeSus(patient.SusNumber))
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                    .ToDictionary(group => group.Key, group => group.ToList()),
                PatientsByName = patientList
                    .Where(patient => !string.IsNullOrWhiteSpace(patient.Name))
                    .GroupBy(patient => Normalize(patient.Name))
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                    .ToDictionary(group => group.Key, group => group.ToList()),
                PatientsByNameAndHouse = patientList
                    .Where(patient => !string.IsNullOrWhiteSpace(patient.Name) && patient.HouseId > 0)
                    .GroupBy(patient => (Name: Normalize(patient.Name), patient.HouseId))
                    .ToDictionary(group => group.Key, group => group.ToList()),
                PatientsByNameAndAddress = contextList
                    .Where(context => !string.IsNullOrWhiteSpace(context.Patient.Name))
                    .SelectMany(context => context.AddressKeys
                        .Where(key => !string.IsNullOrWhiteSpace(key))
                        .Select(key => new { Name = Normalize(context.Patient.Name), AddressKey = key, context.Patient }))
                    .GroupBy(item => (item.Name, item.AddressKey))
                    .ToDictionary(group => group.Key, group => group.Select(item => item.Patient).DistinctBy(patient => patient.Id).ToList())
            };
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
                AddAddressKey(keys, normalizedCep, normalizedStreet, normalizedNumber, string.Empty, string.Empty, string.Empty, string.Empty);
                AddAddressKey(keys, string.Empty, normalizedStreet, normalizedNumber, string.Empty, string.Empty, string.Empty, string.Empty);
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

        private static string BuildSusImportKey(string normalizedSus)
        {
            return $"sus:{normalizedSus}";
        }

        private static string BuildIdentityImportKey(PatientIdentityKey key)
        {
            return $"identity:{key.Name}|{key.MotherName}|{key.BirthDate:yyyyMMdd}";
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

        private sealed class PatientImportRowContext
        {
            public int RowNumber { get; init; }
            public Patient Patient { get; set; } = new();
            public string MotherName { get; init; } = string.Empty;
            public string FatherName { get; init; } = string.Empty;
            public string FamilyResponsibleSus { get; init; } = string.Empty;
            public bool IsFamilyResponsible { get; init; }
            public IReadOnlyList<string> AddressKeys { get; init; } = [];
            public int? ResolvedHouseId { get; set; }
        }

        private sealed class PatientImportIndexes
        {
            public Dictionary<string, List<Patient>> PatientsBySus { get; init; } = [];
            public Dictionary<string, List<Patient>> PatientsByName { get; init; } = [];
            public Dictionary<(string Name, int HouseId), List<Patient>> PatientsByNameAndHouse { get; init; } = [];
            public Dictionary<(string Name, string AddressKey), List<Patient>> PatientsByNameAndAddress { get; init; } = [];
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

        private enum ParentKind
        {
            Mother,
            Father
        }
    }
}
