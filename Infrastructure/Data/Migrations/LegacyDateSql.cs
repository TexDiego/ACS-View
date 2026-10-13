using SQLite;

namespace ACS_View.Infrastructure.Data.Migrations;

internal static class LegacyDateSql
{
    // Previous migrations used CURRENT_TIMESTAMP (TEXT), while sqlite-net expects ticks.
    // Normalize only the SELECT result; never rewrite the historical storage.
    public static string AsTicks(string column) => $"""
        CASE WHEN {column} IS NULL THEN NULL
          WHEN typeof({column}) IN ('integer','real') OR CAST({column} AS TEXT) NOT GLOB '*[^0-9]*'
            THEN CAST({column} AS INTEGER)
          ELSE (CAST(strftime('%s',{column}) AS INTEGER)+62135596800)*10000000
            + CAST(ROUND((CAST(strftime('%f',{column}) AS REAL)-CAST(strftime('%S',{column}) AS INTEGER))*10000000) AS INTEGER)
        END
        """;
    public static string Projection<T>() => string.Join(",",typeof(T).GetProperties()
        .Where(p=>p.CanWrite && !Attribute.IsDefined(p,typeof(IgnoreAttribute)))
        .Select(p=>
        {
            var name=SchemaMigrator.Quote(p.Name);
            return (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType)==typeof(DateTime)
                ? $"{AsTicks(name)} AS {name}" : name;
        }));
}
