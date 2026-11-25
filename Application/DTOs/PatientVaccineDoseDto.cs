using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.Application.DTOs;

public sealed record PatientVaccineDoseDto(
    VaccineDoseDefinition Definition,
    int? ApplicationId,
    DateTime RecommendedDate,
    DateTime? MaximumDate,
    DateTime? ApplicationDate,
    string LotNumber,
    string Notes,
    VaccineStatus Status)
{
    public bool IsApplied => Status is VaccineStatus.Applied or VaccineStatus.AppliedLate || ApplicationDate.HasValue;
    public bool HasAgeBasedSchedule => Status != VaccineStatus.RequiresAssessment && Definition.IsRoutine && !Definition.RequiresAssessment && !Definition.RequiresPregnancy && !Definition.IsSeasonal && !Definition.RepeatIntervalMonths.HasValue;
}
