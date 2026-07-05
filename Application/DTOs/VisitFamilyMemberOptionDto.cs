using ACS_View.Domain.Enums;

namespace ACS_View.Application.DTOs;

public sealed class VisitFamilyMemberOptionDto
{
    public int PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string CareLinesText { get; init; } = string.Empty;
    public VisitStatus Status { get; set; } = VisitStatus.NoVisit;
}
