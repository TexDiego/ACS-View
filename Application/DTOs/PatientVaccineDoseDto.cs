using ACS_View.Domain.ValueObjects;

namespace ACS_View.Application.DTOs;

public sealed record PatientVaccineDoseDto(
    VaccineDoseDefinition Definition,
    bool IsApplied);
