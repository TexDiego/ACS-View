using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class PregnancyCalculator
{
    public const int DefaultPuerperiumDays = 42;

    public static DateTime CalculateExpectedBirthDate(DateTime lastMenstrualPeriod)
    {
        return lastMenstrualPeriod.Date.AddDays(280);
    }

    public static GestationalAge? CalculateGestationalAge(
        PatientPregnancy pregnancy,
        DateTime? referenceDate = null)
    {
        if (pregnancy.LastMenstrualPeriod is null)
        {
            return null;
        }

        var today = (referenceDate ?? DateTime.Today).Date;
        var days = Math.Max(0, (today - pregnancy.LastMenstrualPeriod.Value.Date).Days);
        return new GestationalAge(days / 7, days % 7);
    }

    public static int? CalculateTrimester(PatientPregnancy pregnancy, DateTime? referenceDate = null)
    {
        var age = CalculateGestationalAge(pregnancy, referenceDate);
        if (age is null)
        {
            return null;
        }

        return age.Value.Weeks switch
        {
            < 14 => 1,
            < 28 => 2,
            _ => 3
        };
    }

    public static bool IsDueDateSoon(
        PatientPregnancy pregnancy,
        DateTime? referenceDate = null,
        int daysAhead = 7)
    {
        if (pregnancy.ExpectedBirthDate is null)
        {
            return false;
        }

        var today = (referenceDate ?? DateTime.Today).Date;
        var dueDate = pregnancy.ExpectedBirthDate.Value.Date;
        return dueDate >= today && dueDate <= today.AddDays(daysAhead);
    }

    public static bool IsDueDateOverdue(PatientPregnancy pregnancy, DateTime? referenceDate = null)
    {
        return pregnancy.ExpectedBirthDate is not null &&
               pregnancy.ExpectedBirthDate.Value.Date < (referenceDate ?? DateTime.Today).Date;
    }

    public static bool IsAdvancedPregnancy(PatientPregnancy pregnancy, DateTime? referenceDate = null)
    {
        var age = CalculateGestationalAge(pregnancy, referenceDate);
        return age?.Weeks >= 37;
    }

    public static bool IsPuerperal(
        PatientPregnancy pregnancy,
        DateTime? referenceDate = null,
        int puerperiumDays = DefaultPuerperiumDays)
    {
        if (pregnancy.Status != PregnancyStatus.Ended ||
            pregnancy.EndType != PregnancyEndType.Birth ||
            pregnancy.EndedAt is null)
        {
            return false;
        }

        var today = (referenceDate ?? DateTime.Today).Date;
        var birthDate = pregnancy.EndedAt.Value.Date;
        var daysSinceBirth = (today - birthDate).Days;
        return daysSinceBirth >= 0 && daysSinceBirth <= puerperiumDays;
    }

    public static int? CalculatePostpartumDays(PatientPregnancy pregnancy, DateTime? referenceDate = null)
    {
        if (!IsPuerperal(pregnancy, referenceDate))
        {
            return null;
        }

        return ((referenceDate ?? DateTime.Today).Date - pregnancy.EndedAt!.Value.Date).Days;
    }

    public static DateTime? CalculatePuerperiumEndDate(
        PatientPregnancy pregnancy,
        int puerperiumDays = DefaultPuerperiumDays)
    {
        return pregnancy.EndType == PregnancyEndType.Birth && pregnancy.EndedAt is not null
            ? pregnancy.EndedAt.Value.Date.AddDays(puerperiumDays)
            : null;
    }

    public static int CalculateRegisteredChildrenCount(int patientId, IEnumerable<Patient> patients)
    {
        return patients.Count(patient => patient.MotherPatientId == patientId);
    }
}
