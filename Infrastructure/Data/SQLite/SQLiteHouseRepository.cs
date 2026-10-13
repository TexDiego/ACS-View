using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;
using System.Diagnostics;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLiteHouseRepository(IDatabaseService databaseService, ICurrentUserContext currentUserContext) : IHouseRepository
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

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

    public async Task<List<House>> GetAllAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return await _connection.Table<House>().Where(h => h.UserId == userId && h.IsActive).ToListAsync();
    }

    public async Task<PagedResultDto<HouseListItemDto>> GetListAsync(string? search, int skip, int take, string? filterKey = null)
    {
        try
        {
            skip = Math.Max(skip, 0);
            take = Math.Clamp(take, 1, 100);

            var searchCriteria = BuildSearchCriteria(search, currentUserContext.RequireCurrentUserId(), filterKey);
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

    public Task InsertAsync(House house)
    {
        house.UserId = currentUserContext.RequireCurrentUserId();
        return _connection.InsertAsync(house);
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
            house.RowRevision=c.ExecuteScalar<int>("SELECT RowRevision FROM House WHERE CasaId=? AND UserId=?",house.CasaId,userId);
        });
    }

    private static HouseSearchCriteria BuildSearchCriteria(string? search, int userId, string? filterKey)
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
                      AND COALESCE(p.IsActive, 1) = 1
                )
                """);
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
