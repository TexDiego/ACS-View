using ACS_View.Domain.ValueObjects;

namespace ACS_View.UseCases.Services;

internal static class PatientFilterSqlBuilder
{
    internal static void AddFilterClause(string? filterKey, List<string> whereParts, List<object> parameters)
    {
        if (string.IsNullOrWhiteSpace(filterKey) || filterKey == DashboardFilterKeys.All)
        {
            return;
        }

        if (DashboardFilterKeys.IsCombination(filterKey))
        {
            foreach (var part in DashboardFilterKeys.GetCombinationParts(filterKey))
            {
                AddFilterClause(part, whereParts, parameters);
            }

            return;
        }

        var today = DateTime.Today;

        switch (filterKey)
        {
            case DashboardFilterKeys.NoHouse:
                whereParts.Add("p.HouseId = -1");
                return;
            case DashboardFilterKeys.NoFamily:
                whereParts.Add("p.FamilyId = -1");
                return;
            case DashboardFilterKeys.BolsaFamilia:
                whereParts.Add("p.BolsaFamilia = 1");
                return;
            case DashboardFilterKeys.Elderly:
                whereParts.Add("p.BirthDate <= ?");
                parameters.Add(today.AddYears(-60));
                return;
            case DashboardFilterKeys.ChildrenUnder6:
                whereParts.Add("p.BirthDate > ?");
                parameters.Add(today.AddYears(-6));
                return;
        }

        if (filterKey.StartsWith(DashboardFilterKeys.ConditionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var condition = filterKey[DashboardFilterKeys.ConditionPrefix.Length..];
            if (HealthConditionCatalog.GetKey(condition) == HealthConditionCatalog.Diabetes)
            {
                whereParts.Add("""
                    EXISTS (
                        SELECT 1
                        FROM PatientConditions pc
                        WHERE pc.PatientId = p.Id
                          AND pc.Description LIKE 'Diabetes%' COLLATE NOCASE
                    )
                    """);
                return;
            }

            whereParts.Add("""
                EXISTS (
                    SELECT 1
                    FROM PatientConditions pc
                    WHERE pc.PatientId = p.Id
                      AND pc.Description = ? COLLATE NOCASE
                )
                """);
            parameters.Add(condition);
            return;
        }

        if (filterKey.StartsWith(DashboardFilterKeys.CidPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var cid = filterKey[DashboardFilterKeys.CidPrefix.Length..];
            whereParts.Add("""
                EXISTS (
                    SELECT 1
                    FROM PatientCID pc
                    INNER JOIN CidSubcategory sc ON sc.Id = pc.CidId
                    WHERE pc.PatientId = p.Id
                      AND sc.Code = ? COLLATE NOCASE
                )
                """);
            parameters.Add(cid);
        }
    }
}
