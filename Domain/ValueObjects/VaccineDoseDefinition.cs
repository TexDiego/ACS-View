using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public sealed record VaccineDoseDefinition(
    string DoseKey,
    string VaccineName,
    string DoseLabel,
    VaccineSectionType Section,
    int? MinAgeMonths,
    int? MaxAgeMonths,
    bool IsRequiredForChildMetric = false,
    bool RequiresPregnancy = false,
    string? AgeLabel = null,
    string? DiseaseDescription = null,
    string? Guidance = null,
    bool IsRoutine = true,
    bool RequiresAssessment = false,
    int? AdministrationAgeLimitMonthsExclusive = null,
    bool AvoidDuringPregnancy = false,
    bool IsSeasonal = false,
    int? RepeatIntervalMonths = null,
    int? PregnancyWeek = null,
    bool PerPregnancy = false,
    string? PreviousDoseKey = null,
    int MinimumIntervalDays = 0,
    int MinimumIntervalMonths = 0,
    DateTime? ContextRecommendedDate = null)
{
    public bool AllowsPeriodicApplications => IsSeasonal || RepeatIntervalMonths.HasValue || PerPregnancy ||
        DoseKey.StartsWith("Covid-Periodica-", StringComparison.OrdinalIgnoreCase);
}
