namespace ACS_View.Application.DTOs;

public sealed class PatientVaccineScheduleDto
{
    public int PatientId { get; init; }
    public DateTime BirthDate { get; init; }
    public bool IsPregnant { get; init; }
    public IReadOnlyDictionary<string, PatientVaccineDoseDto> ApplicationsByDose { get; init; } = new Dictionary<string, PatientVaccineDoseDto>();
    public IReadOnlyList<PatientVaccineDoseDto> Doses { get; init; } = [];
}
