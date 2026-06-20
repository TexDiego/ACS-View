using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.UseCases.DTOs;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class PatientService : IPatientService
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly IDatabaseService _db;

        public PatientService(IDatabaseService db)
        {
            _db = db;
            _connection = _db.Connection;
        }

        public async Task DeletePatient(int Id)
        {
            await _connection.RunInTransactionAsync(connection =>
            {
                connection.Execute("DELETE FROM PatientCID WHERE PatientId = ?", Id);
                connection.Execute("DELETE FROM PatientConditions WHERE PatientId = ?", Id);
                connection.Delete<Patient>(Id);
            });

            DataChangeTracker.MarkPatientsChanged();
        }

        public Task<List<Patient>?> GetAllPatients()
        {
            return _connection.Table<Patient>()
                              .OrderBy(n => n.Name)
                              .ToListAsync();
        }

        public async Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take)
        {
            return await GetPatientListAsync(search, skip, take, new PatientListFilterDto());
        }

        public async Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take, string? filterKey)
        {
            return await GetPatientListAsync(search, skip, take, new PatientListFilterDto
            {
                FilterKey = string.IsNullOrWhiteSpace(filterKey) ? "ALL" : filterKey
            });
        }

        public async Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take, PatientListFilterDto filter)
        {
            skip = Math.Max(skip, 0);
            take = Math.Clamp(take, 1, 100);

            filter ??= new PatientListFilterDto();

            var normalizedSearch = search?.Trim() ?? string.Empty;
            var whereParts = new List<string>();
            var parameters = new List<object>();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var likeSearch = $"%{normalizedSearch}%";
                whereParts.Add("(p.Name LIKE ? COLLATE NOCASE OR p.SusNumber LIKE ? COLLATE NOCASE)");
                parameters.Add(likeSearch);
                parameters.Add(likeSearch);
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

        public async Task<List<Patient>?> GetPatientsByCondition(int conditionId)
        {
            return await _connection.Table<Patient>().OrderBy(p => p.Name).ToListAsync();
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

        public async Task<Patient?> GetPatientById(int Id)
        {
            return await _connection.Table<Patient>()
                              .FirstOrDefaultAsync(p => p.Id == Id);
        }

        public async Task CreatePatient(Patient patient)
        {
            await _connection.InsertAsync(patient);
            DataChangeTracker.MarkPatientsChanged();
        }

        public async Task UpdatePatient(Patient patient)
        {
            await _connection.UpdateAsync(patient);
            DataChangeTracker.MarkPatientsChanged();
        }

        public async Task<List<Patient>?> GetPatientsByHouseId(int houseId)
        {
            return await _connection.Table<Patient>().Where(p => p.HouseId == houseId).ToListAsync();
        }

        public async Task<List<Patient>?> GetPatientsByFamilyAndHouseId(int familyId, int houseId)
        {
            return await _connection.Table<Patient>()
                                    .Where(p => p.FamilyId == familyId && p.HouseId == houseId)
                                    .ToListAsync();
        }
    }
}
