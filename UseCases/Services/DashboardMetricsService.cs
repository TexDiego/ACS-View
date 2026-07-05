using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Application.Querying;
using ACS_View.Domain.ValueObjects;
using ACS_View.Application.DTOs;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class DashboardMetricsService(IDatabaseService db, ICurrentUserContext currentUserContext) : IDashboardMetricsService
    {
        private const string ActivePatientClause = "COALESCE(IsActive, 1) = 1";
        private const string ActivePatientAliasClause = "COALESCE(p.IsActive, 1) = 1";

        private readonly SQLiteAsyncConnection _connection = db.Connection;

        public async Task<DashboardMetricsDto> GetMetricsAsync()
        {
            var userId = currentUserContext.RequireCurrentUserId();
            var today = DateTime.Today;
            var elderlyCutoff = today.AddYears(-60);
            var childrenUnder6Cutoff = today.AddYears(-6);

            var metrics = new DashboardMetricsDto()
            {
                TotalPacientes = await _connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM Patient WHERE UserId = ? AND {ActivePatientClause}",
                    userId),
                TotalResidencias = await _connection.Table<House>().CountAsync(h => h.UserId == userId),
                TotalFamilias = await _connection.ExecuteScalarAsync<int>(
                    $"""
                    SELECT COUNT(*)
                    FROM (
                        SELECT p.HouseId, p.FamilyId
                        FROM Patient p
                        WHERE p.UserId = ?
                          AND {ActivePatientAliasClause}
                          AND p.HouseId > 0
                          AND p.FamilyId > 0
                        GROUP BY p.HouseId, p.FamilyId
                    ) families
                    """,
                    userId),
                TotalResidenciasVazias = await _connection.ExecuteScalarAsync<int>(
                    $"""
                    SELECT COUNT(*)
                    FROM House h
                    WHERE h.UserId = ?
                      AND NOT EXISTS (
                          SELECT 1
                          FROM Patient p
                          WHERE p.UserId = h.UserId
                            AND p.HouseId = h.CasaId
                            AND {ActivePatientAliasClause}
                      )
                    """,
                    userId),
                TotalSemResidencia = await _connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM Patient WHERE UserId = ? AND {ActivePatientClause} AND HouseId = -1",
                    userId),
                TotalBolsaFamilia = await _connection.ExecuteScalarAsync<int>(
                    $"""
                    SELECT COUNT(DISTINCT p.Id)
                    FROM Patient p
                    INNER JOIN PatientBolsaFamilia bf ON bf.UserId = p.UserId AND bf.PatientId = p.Id
                    WHERE p.UserId = ?
                      AND {ActivePatientAliasClause}
                    """,
                    userId),
                TotalIdosos = await _connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM Patient WHERE UserId = ? AND {ActivePatientClause} AND BirthDate <= ?",
                    userId,
                    elderlyCutoff),
                TotalCriancasMenoresDe6 = await _connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM Patient WHERE UserId = ? AND {ActivePatientClause} AND BirthDate > ?",
                    userId,
                    childrenUnder6Cutoff),
                TotalCriancasVacinacaoAtrasada = await CountPatientsByFilterAsync(DashboardFilterKeys.ChildrenOverdueVaccines),
                TotalMulheres25a64 = await _connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM Patient WHERE UserId = ? AND {ActivePatientClause} AND Sexo = ? COLLATE NOCASE AND BirthDate <= ? AND BirthDate > ?",
                    userId,
                    "Feminino",
                    today.AddYears(-25),
                    today.AddYears(-65)),
                TotalInativos = await _connection.Table<Patient>().CountAsync(p => p.UserId == userId && !p.IsActive)
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
                WHERE pc.UserId = ?
                  AND p.UserId = ?
                  AND COALESCE(p.IsActive, 1) = 1
                GROUP BY sc.Code, sc.Description
                ORDER BY Quantity DESC, sc.Code";

            var userId = currentUserContext.RequireCurrentUserId();
            return await _connection.QueryAsync<DashboardCidsDTO>(query, userId, userId);
        }

        public async Task<List<ConditionsDTO>> GetConditionsAsync()
        {
            const string query = @"
                SELECT pc.Description AS Description,
                       COALESCE(COUNT(DISTINCT pc.PatientId), 0) AS Quantity
                FROM PatientConditions pc
                INNER JOIN Patient p ON p.Id = pc.PatientId
                WHERE pc.PatientId IS NOT NULL
                  AND pc.UserId = ?
                  AND p.UserId = ?
                  AND COALESCE(p.IsActive, 1) = 1
                GROUP BY pc.Description
                ORDER BY Quantity DESC;";

            var userId = currentUserContext.RequireCurrentUserId();
            var savedConditions = await _connection.QueryAsync<ConditionsDTO>(query, userId, userId);
            var groupedConditions = savedConditions
                .Where(condition => HealthConditionCatalog.IsStandardCondition(condition.Description))
                .GroupBy(condition => HealthConditionCatalog.GetDisplayName(condition.Description), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(condition => condition.Quantity),
                    StringComparer.OrdinalIgnoreCase);

            var conditions = HealthConditionCatalog.Conditions
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

            conditions.Add(new ConditionsDTO
            {
                Description = HealthConditionCatalog.Insulinodependente,
                Quantity = await _connection.ExecuteScalarAsync<int>(
                    $"""
                    SELECT COUNT(DISTINCT p.Id)
                    FROM Patient p
                    INNER JOIN PatientInsulinDependency pid ON pid.UserId = p.UserId AND pid.PatientId = p.Id
                    WHERE p.UserId = ?
                      AND {ActivePatientAliasClause}
                    """,
                    userId)
            });

            return conditions;
        }

        public async Task<int> CountPatientsByFilterAsync(string filterKey)
        {
            var whereParts = new List<string> { "p.UserId = ?" };
            var parameters = new List<object> { currentUserContext.RequireCurrentUserId() };

            PatientFilterSqlBuilder.AddFilterClause(filterKey, whereParts, parameters);

            var whereClause = whereParts.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", whereParts)}";

            return await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(DISTINCT p.Id) FROM Patient p {whereClause}",
                [.. parameters]);
        }
    }
}
