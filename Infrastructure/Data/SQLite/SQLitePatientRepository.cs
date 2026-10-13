using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Application.Querying;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;
using System.Diagnostics;
using ACS_View.Infrastructure.Data.Migrations;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLitePatientRepository(IDatabaseService databaseService, ICurrentUserContext currentUserContext) : IPatientRepository
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public async Task DeleteAsync(int id)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        // Archive individual patients; clinical and visit history must remain recoverable.
        await _connection.RunInTransactionAsync(connection =>
        {
            connection.Execute("UPDATE Patient SET IsActive=0, StatusReason='Arquivado', StatusChangedAt=? WHERE Id=? AND UserId=?",DateTime.UtcNow,id,userId);
        });
    }

    public async Task<List<Patient>?> GetAllAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var patients = await _connection.Table<Patient>()
            .Where(p => p.UserId == userId)
            .OrderBy(n => n.Name)
            .ToListAsync();
        await LoadSusRecordsAsync(patients, userId);
        return patients;
    }

    public async Task<PagedResultDto<PatientListItemDto>> GetListAsync(string? search, int skip, int take, PatientListFilterDto filter)
    {
        try
        {
            skip = Math.Max(skip, 0);
            take = Math.Clamp(take, 1, 100);
            filter ??= new PatientListFilterDto();

            var normalizedSearch = search?.Trim() ?? string.Empty;
            var userId = currentUserContext.RequireCurrentUserId();
            var whereParts = new List<string> { "p.UserId = ?" };
            var parameters = new List<object> { userId };

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var likeSearch = $"%{SearchTextNormalizer.Normalize(normalizedSearch)}%";
                var rawLikeSearch = $"%{normalizedSearch}%";
                whereParts.Add($"""
                    (
                        p.SearchName LIKE ?
                        OR p.SearchMotherName LIKE ?
                        OR p.SearchFatherName LIKE ?
                        OR (p.SusNumber LIKE ? COLLATE NOCASE OR EXISTS (
                            SELECT 1 FROM PatientSusNumber s WHERE s.PatientId=p.Id AND s.UserId=p.UserId AND s.Number LIKE ?))
                        OR EXISTS (
                            SELECT 1
                            FROM House h
                            WHERE h.UserId = p.UserId
                              AND h.CasaId = p.HouseId
                              AND ({BuildHouseAddressSearchClause("h")})
                        )
                    )
                    """);
                parameters.Add(likeSearch);
                parameters.Add(likeSearch);
                parameters.Add(likeSearch);
                parameters.Add(rawLikeSearch);
                var digits=string.Concat(normalizedSearch.Where(char.IsDigit));
                parameters.Add(digits.Length==0 ? "no-cns-match" : $"%{digits}%");
                AddHouseAddressSearchParameters(parameters, SearchTextNormalizer.Normalize(normalizedSearch));
            }

            PatientFilterSqlBuilder.AddFilterClause(filter.FilterKey, whereParts, parameters);
            AddAgeClause(filter, whereParts, parameters);
            AddBolsaFamiliaClause(filter, whereParts);
            AddSexClause(filter, whereParts, parameters);
            AddMissingInformationClauses(filter, whereParts);

            var whereClause = whereParts.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", whereParts)}";
            var orderBy = GetOrderByClause(filter);

            var totalCount = await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Patient p {whereClause}",
                [.. parameters]);

            parameters.Add(take);
            parameters.Add(skip);

            var items = await _connection.QueryAsync<PatientListItemDto>(
                $"""
                SELECT p.Id, p.SusNumber, p.Name,
                    COALESCE((SELECT s.Number FROM PatientSusNumber s
                        WHERE s.PatientId=p.Id AND s.UserId=p.UserId AND s.IsPrimary=1
                        LIMIT 1), '') AS PrimarySusNumber
                FROM Patient p
                {whereClause}
                ORDER BY {orderBy}
                LIMIT ? OFFSET ?
                """,
                [.. parameters]);

            return new PagedResultDto<PatientListItemDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao buscar pacientes paginados: {ex.Message}");
            return new PagedResultDto<PatientListItemDto>();
        }
    }

    public async Task<List<Patient>?> GetByConditionAsync(int conditionId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var patients = await _connection.QueryAsync<Patient>(
            """
            SELECT p.*
            FROM Patient p
            INNER JOIN PatientConditions pc ON pc.PatientId = p.Id
            WHERE p.UserId = ?
              AND pc.UserId = ?
              AND pc.Id = ?
              AND COALESCE(p.IsActive, 1) = 1
            ORDER BY p.Name COLLATE NOCASE, p.Id
            """,
            userId,
            userId,
            conditionId);
        await LoadSusRecordsAsync(patients, userId);
        return patients;
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var patient = await _connection.Table<Patient>().FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (patient is not null)
            patient.SusNumberRecords = await _connection.Table<PatientSusNumber>()
                .Where(n => n.UserId == userId && n.PatientId == id).ToListAsync();
        return patient;
    }

    private async Task LoadSusRecordsAsync(List<Patient> patients, int userId)
    {
        var records = (await _connection.Table<PatientSusNumber>().Where(n => n.UserId == userId).ToListAsync()).ToLookup(n => n.PatientId);
        foreach (var patient in patients) patient.SusNumberRecords = records[patient.Id].ToList();
    }

    public Task InsertAsync(Patient patient)
    {
        patient.UserId = currentUserContext.RequireCurrentUserId();
        return _connection.RunInTransactionAsync(c => { c.Insert(patient); RelationalStore.SyncPatient(c,patient); });
    }

    public Task UpdateAsync(Patient patient)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.RunInTransactionAsync(c =>
        {
            var original=c.Table<Patient>().FirstOrDefault(p=>p.Id==patient.Id && p.UserId==userId);
            if(original==null) throw new InvalidOperationException("Paciente não pertence à conta atual.");
            if(original.RowRevision!=patient.RowRevision)
                throw new InvalidOperationException("O cadastro foi alterado desde a leitura. Recarregue antes de salvar.");
            patient.UserId=userId;
            if (original.SusNumber != patient.SusNumber) RelationalStore.IssueLegacySus(c, original);
            // sqlite-net materializes NULL scalars using model defaults. Write only fields
            // actually changed, so editing CNS/family cannot replace unknown dates or text.
            var changed=typeof(Patient).GetProperties().Where(p=>p.CanWrite && p.Name is not "Id" and not "UserId" and not "RowRevision"
                && !Attribute.IsDefined(p,typeof(IgnoreAttribute)) && !Equals(p.GetValue(original),p.GetValue(patient))).ToList();
            if(changed.Count>0)
            {
                var values=changed.Select(p=>p.GetValue(patient)!).ToList();values.Add(patient.Id);values.Add(userId);
                c.Execute($"UPDATE Patient SET {string.Join(",",changed.Select(p=>$"{SchemaMigrator.Quote(p.Name)}=?"))} WHERE Id=? AND UserId=?",values.ToArray());
            }
            RelationalStore.SyncPatient(c,patient);
            patient.RowRevision=c.ExecuteScalar<int>("SELECT RowRevision FROM Patient WHERE Id=? AND UserId=?",patient.Id,userId);
        });
    }

    public async Task<List<Patient>?> GetByHouseIdAsync(int houseId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var patients = await _connection.Table<Patient>().Where(p => p.HouseId == houseId && p.UserId == userId).ToListAsync();
        await LoadSusRecordsAsync(patients, userId);
        return patients;
    }

    public async Task<List<Patient>?> GetByFamilyAndHouseIdAsync(int familyId, int houseId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var patients = await _connection.Table<Patient>()
            .Where(p => p.FamilyId == familyId && p.HouseId == houseId && p.UserId == userId)
            .ToListAsync();
        await LoadSusRecordsAsync(patients, userId);
        return patients;
    }

    private static void AddAgeClause(PatientListFilterDto filter, List<string> whereParts, List<object> parameters)
    {
        var today = DateTime.Today;

        if (filter.MinimumAge.HasValue)
        {
            whereParts.Add("p.BirthDate <= ?");
            parameters.Add(today.AddYears(-Math.Max(0, filter.MinimumAge.Value)));
        }

        if (filter.MaximumAge.HasValue)
        {
            whereParts.Add("p.BirthDate >= ?");
            parameters.Add(today.AddYears(-(Math.Max(0, filter.MaximumAge.Value) + 1)).AddDays(1));
        }
    }

    private static void AddSexClause(PatientListFilterDto filter, List<string> whereParts, List<object> parameters)
    {
        var sexes = filter.Sexes
            .Where(sex => !string.IsNullOrWhiteSpace(sex))
            .Select(sex => sex.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sexes.Count == 0)
        {
            return;
        }

        whereParts.Add($"p.Sexo COLLATE NOCASE IN ({string.Join(", ", sexes.Select(_ => "?"))})");
        parameters.AddRange(sexes);
    }

    private static void AddBolsaFamiliaClause(PatientListFilterDto filter, List<string> whereParts)
    {
        if (!filter.OnlyBolsaFamilia)
        {
            return;
        }

        whereParts.Add("""
            EXISTS (
                SELECT 1
                FROM PatientBolsaFamilia bf
                WHERE bf.PatientId = p.Id
                  AND bf.UserId = p.UserId
            )
            """);
    }

    private static void AddMissingInformationClauses(PatientListFilterDto filter, List<string> whereParts)
    {
        // Legacy CNS evidence and any relational CNS (including secondary numbers) count as information.
        if (filter.WithoutSusNumber)
            whereParts.Add("""
                TRIM(COALESCE(p.SusNumber, ''), char(9) || char(10) || char(13) || ' ') = ''
                AND NOT EXISTS (
                    SELECT 1 FROM PatientSusNumber s
                    WHERE s.PatientId = p.Id AND s.UserId = p.UserId
                      AND TRIM(COALESCE(s.Number, ''), char(9) || char(10) || char(13) || ' ') <> ''
                )
                """);

        if (filter.WithoutPhone)
            whereParts.Add("""
                NOT EXISTS (
                    SELECT 1 FROM PatientPhone ph
                    WHERE ph.PatientId = p.Id AND ph.UserId = p.UserId
                      AND TRIM(COALESCE(ph.Number, ''), char(9) || char(10) || char(13) || ' ') <> ''
                )
                """);

        if (filter.WithoutMotherName)
            whereParts.Add("TRIM(COALESCE(p.MotherName, ''), char(9) || char(10) || char(13) || ' ') = ''");
        if (filter.WithoutFatherName)
            whereParts.Add("TRIM(COALESCE(p.FatherName, ''), char(9) || char(10) || char(13) || ' ') = ''");
        if (filter.WithoutHouse)
            whereParts.Add("NOT EXISTS (SELECT 1 FROM House h WHERE h.CasaId = p.HouseId AND h.UserId = p.UserId)");
    }

    private static string GetOrderByClause(PatientListFilterDto filter)
    {
        if (filter.SortBy == PatientListSortOption.Age)
        {
            return filter.SortDescending
                ? "p.BirthDate ASC, p.Name COLLATE NOCASE, p.Id"
                : "p.BirthDate DESC, p.Name COLLATE NOCASE, p.Id";
        }

        return filter.SortDescending
            ? "p.Name COLLATE NOCASE DESC, p.Id DESC"
            : "p.Name COLLATE NOCASE, p.Id";
    }

    private static string BuildHouseAddressSearchClause(string alias)
    {
        return $"""
            {alias}.SearchRua LIKE ?
            OR TRIM(
                COALESCE({alias}.SearchRua, '') ||
                CASE
                    WHEN COALESCE(TRIM({alias}.NumeroCasa), '') <> ''
                    THEN ', ' || TRIM({alias}.NumeroCasa)
                    ELSE ''
                END ||
                CASE
                    WHEN COALESCE(TRIM({alias}.SearchComplemento), '') <> ''
                    THEN ' - ' || TRIM({alias}.SearchComplemento)
                    ELSE ''
                END
            ) LIKE ?
            OR TRIM(
                COALESCE({alias}.SearchRua, '') || ' ' ||
                COALESCE(TRIM({alias}.NumeroCasa), '') || ' ' ||
                COALESCE(TRIM({alias}.SearchComplemento), '')
            ) LIKE ?
            """;
    }

    private static void AddHouseAddressSearchParameters(List<object> parameters, string normalizedSearch)
    {
        var likeSearch = $"%{normalizedSearch}%";
        parameters.Add(likeSearch);
        parameters.Add(likeSearch);
        parameters.Add(likeSearch);
    }
}
