using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class VaccineStatusCalculator
{
    public static VaccineStatus Calculate(
        DateTime birthDate,
        DateTime referenceDate,
        VaccineDoseDefinition definition,
        DateTime? applicationDate)
    {
        var maximumDate = GetMaximumDate(birthDate, definition);

        if (applicationDate.HasValue)
        {
            return maximumDate.HasValue && applicationDate.Value.Date > maximumDate.Value.Date
                ? VaccineStatus.AppliedLate
                : VaccineStatus.Applied;
        }

        if (!definition.IsRoutine ||
            (definition.AdministrationAgeLimitMonthsExclusive is int limit && referenceDate.Date >= birthDate.Date.AddMonths(limit)))
        {
            return VaccineStatus.OutsideRoutine;
        }

        var recommendedDate = GetRecommendedDate(birthDate, definition);
        if (referenceDate.Date < recommendedDate.Date)
        {
            return VaccineStatus.NotYetDue;
        }

        if (definition.RequiresAssessment || definition.RequiresPregnancy || definition.IsSeasonal || definition.RepeatIntervalMonths.HasValue)
        {
            return VaccineStatus.RequiresAssessment;
        }

        if (maximumDate.HasValue && referenceDate.Date > maximumDate.Value.Date)
        {
            return VaccineStatus.Late;
        }

        return VaccineStatus.Due;
    }

    public static DateTime GetRecommendedDate(DateTime birthDate, VaccineDoseDefinition definition)
    {
        if (definition.ContextRecommendedDate is DateTime date) return date.Date;
        return definition.MinAgeMonths.HasValue
            ? birthDate.Date.AddMonths(definition.MinAgeMonths.Value)
            : birthDate.Date;
    }

    public static DateTime? GetMaximumDate(DateTime birthDate, VaccineDoseDefinition definition)
    {
        return definition.MaxAgeMonths.HasValue
            ? birthDate.Date.AddMonths(definition.MaxAgeMonths.Value)
            : null;
    }
}
