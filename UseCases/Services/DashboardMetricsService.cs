using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Application.Querying;
using ACS_View.Domain.ValueObjects;
using ACS_View.Application.DTOs;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class DashboardMetricsService(IDatabaseService db) : IDashboardMetricsService
    {
        private readonly SQLiteAsyncConnection _connection = db.Connection;

        public async Task<DashboardMetricsDto> GetMetricsAsync()
        {
            var today = DateTime.Today;
            var elderlyCutoff = today.AddYears(-60);
            var childrenUnder6Cutoff = today.AddYears(-6);

            var metrics = new DashboardMetricsDto()
            {
                TotalPacientes = await _connection.Table<Patient>().CountAsync(),
                TotalResidencias = await _connection.Table<House>().CountAsync(),
                TotalSemResidencia = await _connection.Table<Patient>().CountAsync(p => p.HouseId == -1),
                TotalBolsaFamilia = await _connection.Table<Patient>().CountAsync(p => p.BolsaFamilia),
                TotalSemFamilia = await _connection.Table<Patient>().CountAsync(p => p.FamilyId == -1),
                TotalIdosos = await _connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Patient WHERE BirthDate <= ?",
                    elderlyCutoff),
                TotalCriancasMenoresDe6 = await _connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Patient WHERE BirthDate > ?",
                    childrenUnder6Cutoff)
            };
            return metrics;
        }

        public async Task<List<DashboardCidsDTO>> GetCidMetricsAsync()
        {
            const string query = @"
                SELECT sc.Code AS Cid,
                       sc.Description AS Description,
                       COALESCE(COUNT(DISTINCT pc.PatientId), 0) AS Quantity
                FROM PatientCID pc
                INNER JOIN Patient p ON p.Id = pc.PatientId
                INNER JOIN CidSubcategory sc ON sc.Id = pc.CidId
                GROUP BY sc.Code, sc.Description
                ORDER BY Quantity DESC, sc.Code";

            return await _connection.QueryAsync<DashboardCidsDTO>(query);
        }

        public async Task<List<ConditionsDTO>> GetConditionsAsync()
        {
            const string query = @"
                SELECT pc.Description AS Description,
                       COALESCE(COUNT(DISTINCT pc.PatientId), 0) AS Quantity
                FROM PatientConditions pc
                INNER JOIN Patient p ON p.Id = pc.PatientId
                WHERE pc.PatientId IS NOT NULL
                GROUP BY pc.Description
                ORDER BY Quantity DESC;";

            var savedConditions = await _connection.QueryAsync<ConditionsDTO>(query);
            var groupedConditions = savedConditions
                .Where(condition => HealthConditionCatalog.IsStandardCondition(condition.Description))
                .GroupBy(condition => HealthConditionCatalog.GetDisplayName(condition.Description), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(condition => condition.Quantity),
                    StringComparer.OrdinalIgnoreCase);

            return HealthConditionCatalog.Conditions
                .Where(condition => !string.Equals(
                    condition,
                    HealthConditionCatalog.BolsaFamilia,
                    StringComparison.OrdinalIgnoreCase))
                .Select(condition => new ConditionsDTO
                {
                    Description = condition,
                    Quantity = groupedConditions.TryGetValue(condition, out var quantity) ? quantity : 0
                })
                .ToList();
        }

        public async Task<int> CountPatientsByFilterAsync(string filterKey)
        {
            var whereParts = new List<string>();
            var parameters = new List<object>();

            PatientFilterSqlBuilder.AddFilterClause(filterKey, whereParts, parameters);

            var whereClause = whereParts.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", whereParts)}";

            return await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Patient p {whereClause}",
                [.. parameters]);
        }
    }
}
