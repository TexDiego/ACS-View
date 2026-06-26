using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Application.Querying;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLitePatientRepository(IDatabaseService databaseService, ICurrentUserContext currentUserContext) : IPatientRepository
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public async Task DeleteAsync(int id)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        await _connection.RunInTransactionAsync(connection =>
        {
            connection.Execute("DELETE FROM PatientCID WHERE PatientId = ? AND UserId = ?", id, userId);
            connection.Execute("DELETE FROM PatientConditions WHERE PatientId = ? AND UserId = ?", id, userId);
            connection.Execute("DELETE FROM Vaccines WHERE Id = ? AND UserId = ?", id, userId);
            connection.Execute("DELETE FROM Patient WHERE Id = ? AND UserId = ?", id, userId);
        });
    }

    public Task<List<Patient>?> GetAllAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<Patient>()
            .Where(p => p.UserId == userId)
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<PagedResultDto<PatientListItemDto>> GetListAsync(string? search, int skip, int take, PatientListFilterDto filter)
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
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<Patient>()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public Task<Patient?> GetByIdAsync(int id)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<Patient>().FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
    }

    public Task InsertAsync(Patient patient)
    {
        patient.UserId = currentUserContext.RequireCurrentUserId();
        return _connection.InsertAsync(patient);
    }

    public Task UpdateAsync(Patient patient)
    {
        patient.UserId = currentUserContext.RequireCurrentUserId();
        return _connection.UpdateAsync(patient);
    }

    public Task<List<Patient>?> GetByHouseIdAsync(int houseId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<Patient>().Where(p => p.HouseId == houseId && p.UserId == userId).ToListAsync();
    }

    public Task<List<Patient>?> GetByFamilyAndHouseIdAsync(int familyId, int houseId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<Patient>()
            .Where(p => p.FamilyId == familyId && p.HouseId == houseId && p.UserId == userId)
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
