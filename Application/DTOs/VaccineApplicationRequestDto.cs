namespace ACS_View.Application.DTOs;

public sealed record VaccineApplicationRequestDto(
    int PatientId,
    string DoseKey,
    DateTime ApplicationDate,
    string LotNumber,
    string Notes);
