namespace ACS_View.Application.DTOs;

public sealed class VisitRecordFamilyGroupDto
{
    public int HouseId { get; init; }
    public int FamilyId { get; init; }
    public string FamilyName { get; init; } = string.Empty;
    public List<VisitRecordDto> Visits { get; init; } = [];
}

public sealed class VisitRecordDto
{
    public int Id { get; init; }
    public int PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public DateTime VisitDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string CareLinesText { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}
