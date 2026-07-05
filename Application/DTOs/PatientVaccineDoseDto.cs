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
    public bool IsApplied => ApplicationDate.HasValue;
}
