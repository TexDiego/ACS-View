using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public sealed record VaccineDoseDefinition(
    string DoseKey,
    string VaccineName,
    string DoseLabel,
    VaccineSectionType Section,
    int? MinAgeMonths,
    int? MaxAgeMonths,
    bool RequiresPregnancy = false,
    string? AgeLabel = null,
    string? DiseaseDescription = null);
