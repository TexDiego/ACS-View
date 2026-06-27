using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ACS_View.UseCases.Services;

internal sealed class PatientFamilyLinkResolver(
    IPatientService patientService,
    IHouseService houseService,
    IFamilyService familyService)
{
    public async Task<int> ResolveAsync(
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

    private static string NormalizeSus(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : Regex.Replace(value, @"\D", string.Empty);
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

    private sealed class PatientImportIndexes
    {
        public Dictionary<string, List<Patient>> PatientsBySus { get; init; } = [];
        public Dictionary<string, List<Patient>> PatientsByName { get; init; } = [];
        public Dictionary<(string Name, int HouseId), List<Patient>> PatientsByNameAndHouse { get; init; } = [];
        public Dictionary<(string Name, string AddressKey), List<Patient>> PatientsByNameAndAddress { get; init; } = [];
    }

    private enum ParentKind
    {
        Mother,
        Father
    }
}

internal sealed class PatientImportRowContext
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
