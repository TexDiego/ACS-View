using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class PregnancyRiskSuggestionCalculator
{
    public static PregnancyRiskSuggestion Calculate(
        Patient patient,
        PatientPregnancy pregnancy,
        IEnumerable<string> conditionDescriptions,
        int registeredChildrenCount,
        DateTime? referenceDate = null)
    {
        var suggestion = new PregnancyRiskSuggestion();
        var today = (referenceDate ?? DateTime.Today).Date;
        var conditions = conditionDescriptions
            .Select(HealthConditionCatalog.GetKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        AddIf(suggestion, conditions.Contains(HealthConditionCatalog.Hipertensao), 2, "hipertensao cadastrada");
        AddIf(suggestion, conditions.Contains(HealthConditionCatalog.Diabetes), 2, "diabetes cadastrada");

        var age = CalculateAge(patient.BirthDate, today);
        AddIf(suggestion, age < 15, 4, "menor de 15 anos");
        AddIf(suggestion, age >= 15 && age < 20, 1, "gestante adolescente");
        AddIf(suggestion, age >= 35, 2, "35 anos ou mais");

        var childrenCount = pregnancy.InformedChildrenCount ?? registeredChildrenCount;
        AddIf(suggestion, childrenCount >= 4, 2, $"{childrenCount} filhos");
        AddIf(suggestion, pregnancy.LastMenstrualPeriod is null && pregnancy.ExpectedBirthDate is null, 1, "DUM e DPP ausentes");
        AddIf(suggestion, PregnancyCalculator.IsDueDateSoon(pregnancy, today, 30), 1, "DPP nos proximos 30 dias");
        AddIf(suggestion, PregnancyCalculator.IsAdvancedPregnancy(pregnancy, today), 1, "gestacao avancada");

        suggestion.Risk = suggestion.Score >= 4
            ? PregnancyRisk.HighRisk
            : suggestion.Score >= 1
                ? PregnancyRisk.Attention
                : PregnancyRisk.Usual;

        return suggestion;
    }

    private static void AddIf(PregnancyRiskSuggestion suggestion, bool condition, int points, string reason)
    {
        if (!condition)
        {
            return;
        }

        suggestion.Score += points;
        suggestion.Reasons.Add(reason);
    }

    private static int CalculateAge(DateTime birthDate, DateTime referenceDate)
    {
        var age = referenceDate.Year - birthDate.Date.Year;
        if (birthDate.Date > referenceDate.AddYears(-age))
        {
            age--;
        }

        return Math.Max(0, age);
    }
}
