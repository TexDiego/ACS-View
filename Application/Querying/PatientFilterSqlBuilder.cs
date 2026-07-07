using ACS_View.Domain.ValueObjects;
using System.Globalization;

namespace ACS_View.Application.Querying;

public static class PatientFilterSqlBuilder
{
    private const string PatientBirthDateSql = """
        CASE
            WHEN p.BirthDate IS NULL
                THEN NULL
            WHEN typeof(p.BirthDate) IN ('integer', 'real')
                 OR CAST(p.BirthDate AS INTEGER) > 100000000000
                THEN date((CAST(p.BirthDate AS INTEGER) / 10000000) - 62135596800, 'unixepoch')
            WHEN CAST(p.BirthDate AS TEXT) GLOB '??/??/????*'
                THEN date(substr(CAST(p.BirthDate AS TEXT), 7, 4) || '-' || substr(CAST(p.BirthDate AS TEXT), 4, 2) || '-' || substr(CAST(p.BirthDate AS TEXT), 1, 2))
            ELSE date(p.BirthDate)
        END
        """;

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
            case DashboardFilterKeys.Pregnant:
                AddActivePregnancyClause(whereParts);
                return;
            case DashboardFilterKeys.PregnancyDueDateSoon:
                whereParts.Add($"""
                    EXISTS (
                        SELECT 1
                        FROM PatientPregnancy pp
                        WHERE pp.PatientId = p.Id
                          AND pp.UserId = p.UserId
                          AND pp.Status = 1
                          AND pp.ExpectedBirthDate IS NOT NULL
                          AND {NormalizeDateSql("pp.ExpectedBirthDate")} BETWEEN date('now', 'localtime') AND date('now', 'localtime', '+7 days')
                    )
                    """);
                return;
            case DashboardFilterKeys.PregnancyThirdTrimester:
                whereParts.Add($"""
                    EXISTS (
                        SELECT 1
                        FROM PatientPregnancy pp
                        WHERE pp.PatientId = p.Id
                          AND pp.UserId = p.UserId
                          AND pp.Status = 1
                          AND pp.LastMenstrualPeriod IS NOT NULL
                          AND date({NormalizeDateSql("pp.LastMenstrualPeriod")}, '+196 days') <= date('now', 'localtime')
                    )
                    """);
                return;
            case DashboardFilterKeys.PregnancyMissingDates:
                whereParts.Add($"""
                    EXISTS (
                        SELECT 1
                        FROM PatientPregnancy pp
                        WHERE pp.PatientId = p.Id
                          AND pp.UserId = p.UserId
                          AND pp.Status = 1
                          AND pp.LastMenstrualPeriod IS NULL
                          AND pp.ExpectedBirthDate IS NULL
                    )
                    """);
                return;
            case DashboardFilterKeys.PregnancyHighRisk:
                whereParts.Add("""
                    EXISTS (
                        SELECT 1
                        FROM PatientPregnancy pp
                        WHERE pp.PatientId = p.Id
                          AND pp.UserId = p.UserId
                          AND pp.Status = 1
                          AND pp.ManualRisk = 3
                    )
                    """);
                return;
            case DashboardFilterKeys.PregnancyRiskNotInformed:
                whereParts.Add("""
                    EXISTS (
                        SELECT 1
                        FROM PatientPregnancy pp
                        WHERE pp.PatientId = p.Id
                          AND pp.UserId = p.UserId
                          AND pp.Status = 1
                          AND pp.ManualRisk = 0
                    )
                    """);
                return;
            case DashboardFilterKeys.ActivePuerperal:
                whereParts.Add($"""
                    EXISTS (
                        SELECT 1
                        FROM PatientPregnancy pp
                        WHERE pp.PatientId = p.Id
                          AND pp.UserId = p.UserId
                          AND pp.Status = 2
                          AND pp.EndType = 1
                          AND pp.EndedAt IS NOT NULL
                          AND p.Sexo = 'Feminino' COLLATE NOCASE
                          AND {NormalizeDateSql("pp.EndedAt")} <= date('now', 'localtime')
                          AND date({NormalizeDateSql("pp.EndedAt")}, '+42 days') >= date('now', 'localtime')
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
            case DashboardFilterKeys.ChildrenOverdueVaccines:
                AddChildrenOverdueVaccinesClause(whereParts, parameters);
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
            if (HealthConditionCatalog.GetKey(condition) == HealthConditionCatalog.Gestante)
            {
                AddActivePregnancyClause(whereParts);
                return;
            }

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

    private static void AddActivePregnancyClause(List<string> whereParts)
    {
        whereParts.Add($"""
            EXISTS (
                SELECT 1
                FROM PatientPregnancy pp
                WHERE pp.PatientId = p.Id
                  AND pp.UserId = p.UserId
                  AND pp.Status = 1
            )
            AND NOT EXISTS (
                SELECT 1
                FROM PatientPregnancy puerperal
                WHERE puerperal.PatientId = p.Id
                  AND puerperal.UserId = p.UserId
                  AND puerperal.Status = 2
                  AND puerperal.EndType = 1
                  AND puerperal.EndedAt IS NOT NULL
                  AND {NormalizeDateSql("puerperal.EndedAt")} <= date('now', 'localtime')
                  AND date({NormalizeDateSql("puerperal.EndedAt")}, '+42 days') >= date('now', 'localtime')
            )
            """);
    }

    private static string NormalizeDateSql(string columnName)
    {
        return $"""
            CASE
                WHEN {columnName} IS NULL
                    THEN NULL
                WHEN typeof({columnName}) IN ('integer', 'real')
                     OR CAST({columnName} AS INTEGER) > 100000000000
                    THEN date((CAST({columnName} AS INTEGER) / 10000000) - 62135596800, 'unixepoch')
                WHEN CAST({columnName} AS TEXT) GLOB '??/??/????*'
                    THEN date(substr(CAST({columnName} AS TEXT), 7, 4) || '-' || substr(CAST({columnName} AS TEXT), 4, 2) || '-' || substr(CAST({columnName} AS TEXT), 1, 2))
                ELSE date({columnName})
            END
            """;
    }

    private static void AddChildrenOverdueVaccinesClause(List<string> whereParts, List<object> parameters)
    {
        var overdueClauses = new List<string>();

        foreach (var definition in VaccineDoseCatalog.GetRequiredChildDefinitions().Where(definition => definition.MaxAgeMonths.HasValue))
        {
            overdueClauses.Add("""
                (
                    date({0}, '+{1} months') < date('now', 'localtime')
                    AND NOT EXISTS (
                        SELECT 1
                        FROM PatientVaccineDose pvd
                        WHERE pvd.UserId = p.UserId
                          AND pvd.PatientId = p.Id
                          AND pvd.DoseKey = ? COLLATE NOCASE
                          AND COALESCE(pvd.IsApplied, 0) = 1
                    )
                )
                """);
            overdueClauses[^1] = string.Format(
                CultureInfo.InvariantCulture,
                overdueClauses[^1],
                PatientBirthDateSql,
                definition.MaxAgeMonths!.Value);
            parameters.Add(definition.DoseKey);
        }

        if (overdueClauses.Count == 0)
        {
            whereParts.Add("1 = 0");
            return;
        }

        whereParts.Add($"date({PatientBirthDateSql}, '+121 months') > date('now', 'localtime')");
        whereParts.Add($"({string.Join(" OR ", overdueClauses)})");
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
