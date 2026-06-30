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
        return _connection.ExecuteAsync("DELETE FROM House WHERE CasaId = ? AND UserId = ?", house.CasaId, userId);
    }

    public async Task<List<House>> GetAllAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return await _connection.Table<House>().Where(h => h.UserId == userId).ToListAsync();
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
        return _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM House WHERE UserId = ?", userId);
    }

    public Task InsertAsync(House house)
    {
        house.UserId = currentUserContext.RequireCurrentUserId();
        return _connection.InsertAsync(house);
    }

    public Task UpdateAsync(House house)
    {
        house.UserId = currentUserContext.RequireCurrentUserId();
        return _connection.UpdateAsync(house);
    }

    private static HouseSearchCriteria BuildSearchCriteria(string? search, int userId, string? filterKey)
    {
        var parameters = new List<object> { userId };
        var clauses = new List<string> { "UserId = ?" };
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
