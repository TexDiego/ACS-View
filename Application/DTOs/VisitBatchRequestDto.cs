using ACS_View.Domain.Enums;

namespace ACS_View.Application.DTOs;

public sealed class VisitBatchRequestDto
{
    public int HouseId { get; init; }
    public int FamilyId { get; init; }
    public DateTime VisitDate { get; init; } = DateTime.Now;
    public string Notes { get; init; } = string.Empty;
    public List<VisitBatchPersonRequestDto> People { get; init; } = [];
}

public sealed class VisitBatchPersonRequestDto
{
    public int PatientId { get; init; }
    public VisitStatus Status { get; init; }
}
