using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Application.Querying;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLitePatientRepository(IDatabaseService databaseService) : IPatientRepository
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public async Task DeleteAsync(int id)
    {
        await _connection.RunInTransactionAsync(connection =>
        {
            connection.Execute("DELETE FROM PatientCID WHERE PatientId = ?", id);
            connection.Execute("DELETE FROM PatientConditions WHERE PatientId = ?", id);
            connection.Delete<Patient>(id);
        });
    }

    public Task<List<Patient>?> GetAllAsync()
    {
        return _connection.Table<Patient>()
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<PagedResultDto<PatientListItemDto>> GetListAsync(string? search, int skip, int take, PatientListFilterDto filter)
    {
        skip = Math.Max(skip, 0);
        take = Math.Clamp(take, 1, 100);
        filter ??= new PatientListFilterDto();

        var normalizedSearch = search?.Trim() ?? string.Empty;
        var whereParts = new List<string>();
        var parameters = new List<object>();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var likeSearch = $"%{SearchTextNormalizer.Normalize(normalizedSearch)}%";
            var rawLikeSearch = $"%{normalizedSearch}%";
            whereParts.Add("(p.SearchName LIKE ? OR p.SearchMotherName LIKE ? OR p.SearchFatherName LIKE ? OR p.SusNumber LIKE ? COLLATE NOCASE)");
            parameters.Add(likeSearch);
            parameters.Add(likeSearch);
            parameters.Add(likeSearch);
            parameters.Add(rawLikeSearch);
        }

        PatientFilterSqlBuilder.AddFilterClause(filter.FilterKey, whereParts, parameters);
        AddAgeClause(filter, whereParts, parameters);

        var whereClause = whereParts.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", whereParts)}";
        var orderBy = GetOrderByClause(filter);

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM Patient p {whereClause}",
            [.. parameters]);

        parameters.Add(take);
        parameters.Add(skip);

        var items = await _connection.QueryAsync<PatientListItemDto>(
            $"""
            SELECT p.Id, p.SusNumber, p.Name
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

    public Task<List<Patient>?> GetByConditionAsync(int conditionId)
    {
        return _connection.Table<Patient>().OrderBy(p => p.Name).ToListAsync();
    }

    public Task<Patient?> GetByIdAsync(int id)
    {
        return _connection.Table<Patient>().FirstOrDefaultAsync(p => p.Id == id);
    }

    public Task InsertAsync(Patient patient)
    {
        return _connection.InsertAsync(patient);
    }

    public Task UpdateAsync(Patient patient)
    {
        return _connection.UpdateAsync(patient);
    }

    public Task<List<Patient>?> GetByHouseIdAsync(int houseId)
    {
        return _connection.Table<Patient>().Where(p => p.HouseId == houseId).ToListAsync();
    }

    public Task<List<Patient>?> GetByFamilyAndHouseIdAsync(int familyId, int houseId)
    {
        return _connection.Table<Patient>()
            .Where(p => p.FamilyId == familyId && p.HouseId == houseId)
            .ToListAsync();
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
}
