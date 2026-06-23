using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;
using System.Diagnostics;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLiteHouseRepository(IDatabaseService databaseService) : IHouseRepository
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public Task DeleteAsync(House house)
    {
        return _connection.DeleteAsync(house);
    }

    public async Task<List<House>> GetAllAsync()
    {
        return await _connection.Table<House>().ToListAsync();
    }

    public async Task<PagedResultDto<HouseListItemDto>> GetListAsync(string? search, int skip, int take)
    {
        try
        {
            skip = Math.Max(skip, 0);
            take = Math.Clamp(take, 1, 100);

            var searchCriteria = BuildSearchCriteria(search);
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
                ORDER BY Rua COLLATE NOCASE, CAST(NumeroCasa AS INTEGER), NumeroCasa COLLATE NOCASE, Complemento COLLATE NOCASE
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
        return _connection.FindAsync<House>(id);
    }

    public async Task<House?> GetByPatientIdAsync(int id)
    {
        const string query = """
            SELECT h.*
            FROM House h
            JOIN Patient r ON h.CasaId = r.HouseId
            WHERE r.Id = ?
            """;

        return await _connection.FindWithQueryAsync<House>(query, id);
    }

    public async Task<int> GetMaxIdAsync()
    {
        var maxId = await _connection.ExecuteScalarAsync<int?>("SELECT MAX(CasaId) FROM House");
        return maxId ?? 0;
    }

    public Task<int> GetTotalCountAsync()
    {
        return _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM House");
    }

    public Task InsertAsync(House house)
    {
        return _connection.InsertAsync(house);
    }

    public Task UpdateAsync(House house)
    {
        return _connection.UpdateAsync(house);
    }

    private static HouseSearchCriteria BuildSearchCriteria(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return new HouseSearchCriteria();
        }

        var normalizedSearch = SearchTextNormalizer.Normalize(search);
        var searchParts = normalizedSearch.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var possibleNumber = searchParts.LastOrDefault();
        var hasNumber = int.TryParse(possibleNumber, out _);
        var streetSearch = string.Join(" ", searchParts.Take(searchParts.Length - (hasNumber ? 1 : 0))).Trim();
        var parameters = new List<object>();
        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(streetSearch))
        {
            clauses.Add("SearchRua LIKE ?");
            parameters.Add($"%{streetSearch}%");
        }

        if (hasNumber && !string.IsNullOrWhiteSpace(possibleNumber))
        {
            clauses.Add("NumeroCasa = ?");
            parameters.Add(possibleNumber);
        }

        if (clauses.Count == 0)
        {
            clauses.Add("(SearchRua LIKE ? OR NumeroCasa LIKE ? COLLATE NOCASE OR SearchComplemento LIKE ?)");
            var likeSearch = $"%{normalizedSearch}%";
            parameters.Add(likeSearch);
            parameters.Add(likeSearch);
            parameters.Add(likeSearch);
        }

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
}
