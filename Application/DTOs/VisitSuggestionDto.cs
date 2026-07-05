namespace ACS_View.Application.DTOs;

public sealed class VisitSuggestionDto
{
    public int PatientId { get; init; }
    public int HouseId { get; init; }
    public int FamilyId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string FamilyName { get; init; } = string.Empty;
    public string CareLineType { get; init; } = string.Empty;
    public decimal PriorityFactor { get; init; }
    public string PriorityLabel { get; init; } = string.Empty;
    public int RequiredVisits { get; init; }
    public int CompletedVisits { get; init; }
    public int MissingVisits { get; init; }
    public bool HasNoCompletedVisitsInRecommendedPeriod { get; init; }
    public int Points { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
    public bool IsOverdue { get; init; }
    public DateTime? LastVisitDate { get; init; }
}
