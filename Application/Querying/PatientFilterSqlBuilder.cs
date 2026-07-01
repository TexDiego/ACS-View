using ACS_View.Domain.ValueObjects;

namespace ACS_View.Application.Querying;

public static class PatientFilterSqlBuilder
{
    public static void AddFilterClause(string? filterKey, List<string> whereParts, List<object> parameters)
    {
        var baseFilterKey = DashboardFilterKeys.GetBaseFilterKey(filterKey);
        var modifiers = DashboardFilterKeys.GetModifiers(filterKey);

        AddStatusClause(baseFilterKey, whereParts);

        if (!string.IsNullOrWhiteSpace(baseFilterKey) && baseFilterKey != DashboardFilterKeys.All)
        {
            AddBaseFilterClause(baseFilterKey, whereParts, parameters);
        }

        AddModifierClauses(modifiers, whereParts, parameters);
    }

    private static void AddStatusClause(string baseFilterKey, List<string> whereParts)
    {
        whereParts.Add(string.Equals(baseFilterKey, DashboardFilterKeys.Inactive, StringComparison.OrdinalIgnoreCase)
            ? "p.IsActive = 0"
            : "COALESCE(p.IsActive, 1) = 1");
    }

    private static void AddBaseFilterClause(string filterKey, List<string> whereParts, List<object> parameters)
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
                whereParts.Add("""
                    EXISTS (
                        SELECT 1
                        FROM PatientBolsaFamilia bf
                        WHERE bf.PatientId = p.Id
                          AND bf.UserId = p.UserId
                    )
                    """);
                return;
            case DashboardFilterKeys.Elderly:
                whereParts.Add("p.BirthDate <= ?");
                parameters.Add(today.AddYears(-60));
                return;
            case DashboardFilterKeys.ChildrenUnder6:
                whereParts.Add("p.BirthDate > ?");
                parameters.Add(today.AddYears(-6));
                return;
            case DashboardFilterKeys.Women25To64:
                whereParts.Add("p.Sexo = ? COLLATE NOCASE");
                parameters.Add("Feminino");
                whereParts.Add("p.BirthDate <= ?");
                parameters.Add(today.AddYears(-25));
                whereParts.Add("p.BirthDate > ?");
                parameters.Add(today.AddYears(-65));
                return;
            case DashboardFilterKeys.Inactive:
                return;
        }

        if (filterKey.StartsWith(DashboardFilterKeys.ConditionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var condition = filterKey[DashboardFilterKeys.ConditionPrefix.Length..];
            if (HealthConditionCatalog.GetKey(condition) == HealthConditionCatalog.Insulinodependente)
            {
                whereParts.Add("""
                    EXISTS (
                        SELECT 1
                        FROM PatientInsulinDependency pid
                        WHERE pid.PatientId = p.Id
                          AND pid.UserId = p.UserId
                    )
                    """);
                return;
            }

            if (HealthConditionCatalog.GetKey(condition) == HealthConditionCatalog.Diabetes)
            {
                whereParts.Add("""
                    EXISTS (
                        SELECT 1
                        FROM PatientConditions pc
                        WHERE pc.PatientId = p.Id
                          AND pc.UserId = p.UserId
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
                      AND pc.UserId = p.UserId
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
                      AND pc.UserId = p.UserId
                      AND sc.Code = ? COLLATE NOCASE
                )
                """);
            parameters.Add(cid);
        }
    }

    private static void AddModifierClauses(
        DashboardFilterModifiers modifiers,
        List<string> whereParts,
        List<object> parameters)
    {
        if (!string.IsNullOrWhiteSpace(modifiers.Sex))
        {
            whereParts.Add("p.Sexo = ? COLLATE NOCASE");
            parameters.Add(modifiers.Sex.Trim());
        }

        var today = DateTime.Today;
        if (modifiers.MinimumAge.HasValue)
        {
            whereParts.Add("p.BirthDate <= ?");
            parameters.Add(today.AddYears(-modifiers.MinimumAge.Value));
        }

        if (modifiers.MaximumAge.HasValue)
        {
            whereParts.Add("p.BirthDate > ?");
            parameters.Add(today.AddYears(-(modifiers.MaximumAge.Value + 1)));
        }
    }
}
