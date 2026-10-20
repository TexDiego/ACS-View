using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;
using ACS_View.Infrastructure.Data.Migrations;
using System.Diagnostics;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLiteHouseRepository(IDatabaseService databaseService, ICurrentUserContext currentUserContext) : IHouseRepository
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public Task<List<House>> SearchTerritorialAsync(string street)
    {
        var tokens = SearchTextNormalizer.Normalize(street).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var clauses = string.Join(" AND ", tokens.Select(_ => "instr(SearchRua, ?) > 0"));
        if (tokens.Length == 0) throw new ArgumentException("Informe a rua / logradouro.");
        return _connection.QueryAsync<House>($"SELECT * FROM House WHERE UserId=? AND IsActive=1 AND {clauses}",
            new object[] { currentUserContext.RequireCurrentUserId() }.Concat(tokens.Cast<object>()).ToArray());
    }

    private static List<House> ReadSelection(SQLiteConnection c, IEnumerable<int> ids, int userId)
    {
        var houses = new List<House>();
        foreach (var chunk in ids.Distinct().Chunk(400))
            houses.AddRange(c.Query<House>($"SELECT * FROM House WHERE UserId=? AND IsActive=1 AND CasaId IN ({string.Join(",", chunk.Select(_ => "?"))})",
                new object[] { userId }.Concat(chunk.Cast<object>()).ToArray()));
        return houses;
    }

    public async Task<List<House>> GetTerritorialSelectionAsync(IReadOnlyCollection<int> ids)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        List<House> houses = [];
        await _connection.RunInTransactionAsync(c => houses = ReadSelection(c, ids, userId));
        if (houses.Count != ids.Distinct().Count()) throw new InvalidOperationException("Uma residência foi removida ou não pertence à conta atual. Limpe o lote e pesquise novamente.");
        return houses;
    }

    public async Task<int> ApplyTerritoryAsync(IReadOnlyCollection<House> reviewed, string sector, string block, bool onlyUnclassified)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        if (reviewed.Count == 0 || string.IsNullOrWhiteSpace(sector) || string.IsNullOrWhiteSpace(block))
            throw new ArgumentException("Selecione residências e informe setor e quadra.");
        var count = 0;
        await _connection.RunInTransactionAsync(c =>
        {
            if (currentUserContext.RequireCurrentUserId() != userId) throw new InvalidOperationException("A sessão foi alterada.");
            var expected = reviewed.ToDictionary(h => h.CasaId);
            var current = ReadSelection(c, expected.Keys, userId);
            if (current.Count != expected.Count || current.Any(h => expected[h.CasaId].UserId != userId || h.RowRevision != expected[h.CasaId].RowRevision))
                throw new InvalidOperationException("O lote foi alterado desde a revisão. Revise novamente antes de aplicar.");
            foreach (var house in current)
            {
                if (onlyUnclassified && TerritorialRules.IsClassified(house)) continue;
                count += c.Execute("UPDATE House SET SetorCensitario=?, NumeroQuadra=? WHERE CasaId=? AND UserId=? AND IsActive=1", sector.Trim(), block.Trim(), house.CasaId, userId);
                house.SetorCensitario = sector.Trim(); house.NumeroQuadra = block.Trim();
                RelationalStore.RecordHouseTerritory(c, house);
            }
        });
        return count;
    }

    public Task DeleteAsync(House house)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.RunInTransactionAsync(c =>
        {
            // Detach current occupancy, retaining historical visits and residence history.
            c.Execute("UPDATE FamilyResidenceHistory SET EndedAt=? WHERE HouseId=? AND UserId=? AND EndedAt IS NULL",DateTime.UtcNow,house.CasaId,userId);
            c.Execute("UPDATE FamilyRecord SET HouseId=NULL WHERE HouseId=? AND UserId=?",house.CasaId,userId);
            foreach(var patient in c.Table<Patient>().Where(p=>p.HouseId==house.CasaId && p.UserId==userId).ToList())
            {
                patient.HouseId=-1; patient.FamilyId=-1; patient.FamilyResponsiblePatientId=null; patient.FamilyResponsibleSus=null;
                ACS_View.Infrastructure.Data.Migrations.RelationalStore.WritePatientFamilyFields(c,patient);
                ACS_View.Infrastructure.Data.Migrations.RelationalStore.SyncPatient(c,patient);
            }
            // Referenced residences stay as historical records. Removing them would erase evidence.
            c.Execute("UPDATE House SET IsActive=0 WHERE CasaId=? AND UserId=?",house.CasaId,userId);
            if(c.ExecuteScalar<int>("SELECT (SELECT COUNT(*) FROM FamilyResidenceHistory WHERE HouseId=?)+(SELECT COUNT(*) FROM Visits WHERE HouseId=?)+(SELECT COUNT(*) FROM TerritorialAssignment WHERE HouseId=?)",house.CasaId,house.CasaId,house.CasaId)==0)
                c.Execute("DELETE FROM House WHERE CasaId=? AND UserId=?",house.CasaId,userId);
        });
    }

    public Task<List<House>> GetAllAsync(string? search = null)
    {
        // Share the exact address/complement/patient criteria used by the residences page.
        var criteria = BuildSearchCriteria(search, currentUserContext.RequireCurrentUserId(), null);
        return _connection.QueryAsync<House>($"SELECT * FROM House {criteria.WhereClause}", [.. criteria.Parameters]);
    }

    public async Task<PagedResultDto<HouseListItemDto>> GetListAsync(string? search, int skip, int take, string? filterKey = null, HouseNumberFilterDto? numberFilter = null)
    {
        try
        {
            skip = Math.Max(skip, 0);
            take = Math.Clamp(take, 1, 100);

            var searchCriteria = BuildSearchCriteria(search, currentUserContext.RequireCurrentUserId(), filterKey, numberFilter);
            var totalCount = await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM House {searchCriteria.WhereClause}",
                [.. searchCriteria.Parameters]);

            searchCriteria.Parameters.Add(take);
            searchCriteria.Parameters.Add(skip);

            var items = await _connection.QueryAsync<HouseListItemDto>(
                $"""
                SELECT CasaId, Rua, NumeroCasa, Complemento
                FROM House
                {searchCriteria.WhereClause}
                ORDER BY Rua COLLATE NOCASE ASC,
                         CASE WHEN TRIM(NumeroCasa) GLOB '[0-9]*' AND TRIM(NumeroCasa) <> '' THEN 0 ELSE 1 END ASC,
                         CAST(NumeroCasa AS INTEGER) ASC,
                         NumeroCasa COLLATE NOCASE ASC,
                         Complemento COLLATE NOCASE ASC,
                         CasaId ASC
                LIMIT ? OFFSET ?
                """,
                [.. searchCriteria.Parameters]);

            return new PagedResultDto<HouseListItemDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao buscar casas paginadas: {ex.Message}");
            return new PagedResultDto<HouseListItemDto>();
        }
    }

    public Task<House?> GetByIdAsync(int id)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<House>().FirstOrDefaultAsync(h => h.CasaId == id && h.UserId == userId);
    }

    public async Task<House?> GetByPatientIdAsync(int id)
    {
        const string query = """
            SELECT h.*
            FROM House h
            JOIN Patient r ON h.CasaId = r.HouseId
            WHERE r.Id = ?
              AND h.UserId = ?
              AND r.UserId = ?
            """;

        var userId = currentUserContext.RequireCurrentUserId();
        return await _connection.FindWithQueryAsync<House>(query, id, userId, userId);
    }

    public async Task<int> GetMaxIdAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var maxId = await _connection.ExecuteScalarAsync<int?>("SELECT MAX(CasaId) FROM House WHERE UserId = ?", userId);
        return maxId ?? 0;
    }

    public Task<int> GetTotalCountAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM House WHERE UserId = ? AND IsActive=1", userId);
    }

    public async Task InsertAsync(House house)
    {
        house.UserId = currentUserContext.RequireCurrentUserId();
        var originalId = house.CasaId;
        try
        {
            await _connection.RunInTransactionAsync(c =>
            {
                c.Insert(house);
                RelationalStore.RecordHouseTerritory(c, house);
            });
        }
        catch
        {
            // sqlite-net publishes generated IDs before commit. A rolled-back ID must never
            // make the editor retry as an update against an unrelated future residence.
            house.CasaId = originalId;
            throw;
        }
    }

    public Task UpdateAsync(House house)
    {
        var userId=currentUserContext.RequireCurrentUserId();
        return _connection.RunInTransactionAsync(c=>
        {
            var original=c.Table<House>().FirstOrDefault(h=>h.CasaId==house.CasaId && h.UserId==userId);
            if(original==null)
                throw new InvalidOperationException("Residência não pertence à conta atual.");
            if(original.RowRevision!=house.RowRevision)
                throw new InvalidOperationException("A residência foi alterada desde a leitura. Recarregue antes de salvar.");
            house.UserId=userId;
            var changed=typeof(House).GetProperties().Where(p=>p.CanWrite && p.Name is not "CasaId" and not "UserId" and not "RowRevision"
                && !Attribute.IsDefined(p,typeof(IgnoreAttribute)) && !Equals(p.GetValue(original),p.GetValue(house))).ToList();
            if(changed.Count>0)
            {
                var values=changed.Select(p=>p.GetValue(house)!).ToList();values.Add(house.CasaId);values.Add(userId);
                c.Execute($"UPDATE House SET {string.Join(",",changed.Select(p=>$"{ACS_View.Infrastructure.Data.Migrations.SchemaMigrator.Quote(p.Name)}=?"))} WHERE CasaId=? AND UserId=?",values.ToArray());
            }
            RelationalStore.RecordHouseTerritory(c, house);
            house.RowRevision=c.ExecuteScalar<int>("SELECT RowRevision FROM House WHERE CasaId=? AND UserId=?",house.CasaId,userId);
        });
    }

    private static HouseSearchCriteria BuildSearchCriteria(string? search, int userId, string? filterKey, HouseNumberFilterDto? numberFilter = null)
    {
        var parameters = new List<object> { userId };
        var clauses = new List<string> { "UserId = ?", "IsActive=1" };
        var normalizedFilterKey = DashboardFilterKeys.GetBaseFilterKey(filterKey);

        if (string.Equals(normalizedFilterKey, DashboardFilterKeys.EmptyResidences, StringComparison.OrdinalIgnoreCase))
        {
            clauses.Add("""
                NOT EXISTS (
                    SELECT 1
                    FROM Patient p
                    WHERE p.UserId = House.UserId
                      AND p.HouseId = House.CasaId
                      AND p.FamilyId > 0
                      AND COALESCE(p.IsActive, 1) = 1
                )
                """);
        }

        if (numberFilter is { IsActive: true })
        {
            numberFilter.Validate();
            // Same initial decimal component as TerritorialRules.NumericPart: 450-B -> 450.
            // Reject unknown numbers and values outside Int32 before counting or paging.
            const string trimmed = "TRIM(NumeroCasa, char(9,10,11,12,13,32,133,160,5760,8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,8232,8233,8239,8287,12288))";
            const string number = "CAST(" + trimmed + " AS INTEGER)";
            clauses.Add($"{trimmed} GLOB '[0-9]*'");
            clauses.Add($"{number} <= 2147483647");
            if (numberFilter.Start.HasValue)
            {
                clauses.Add($"{number} >= ?");
                parameters.Add(numberFilter.Start.Value);
            }
            if (numberFilter.End.HasValue)
            {
                clauses.Add($"{number} <= ?");
                parameters.Add(numberFilter.End.Value);
            }
            if (numberFilter.Parity != HouseParity.All)
            {
                clauses.Add($"{number} % 2 = ?");
                parameters.Add(numberFilter.Parity == HouseParity.Even ? 0 : 1);
            }
        }

        if (string.IsNullOrWhiteSpace(search))
        {
            return new HouseSearchCriteria
            {
                WhereClause = $"WHERE {string.Join(" AND ", clauses)}",
                Parameters = parameters
            };
        }

        var normalizedSearch = SearchTextNormalizer.Normalize(search);
        var likeSearch = $"%{normalizedSearch}%";

        clauses.Add($"""
            (
                {BuildHouseAddressSearchClause("House")}
                OR EXISTS (
                    SELECT 1
                    FROM Patient p
                    WHERE p.UserId = House.UserId
                      AND p.HouseId = House.CasaId
                      AND COALESCE(p.IsActive, 1) = 1
                      AND p.SearchName LIKE ?
                )
            )
            """);
        AddHouseAddressSearchParameters(parameters, normalizedSearch);
        parameters.Add(likeSearch);

        return new HouseSearchCriteria
        {
            WhereClause = $"WHERE {string.Join(" AND ", clauses)}",
            Parameters = parameters
        };
    }

    private sealed class HouseSearchCriteria
    {
        public string WhereClause { get; init; } = string.Empty;
        public List<object> Parameters { get; init; } = [];
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
