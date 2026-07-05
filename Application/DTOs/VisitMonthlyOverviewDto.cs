namespace ACS_View.Application.DTOs;

public sealed class VisitMonthlyOverviewDto
{
    public int TotalVisits { get; set; }
    public int CompletedVisits { get; set; }
    public int AbsentVisits { get; set; }
    public int RefusedVisits { get; set; }
    public int ChildVisits { get; set; }
    public int PregnancyPostpartumVisits { get; set; }
    public int HypertensionVisits { get; set; }
    public int DiabetesVisits { get; set; }
    public int ElderlyVisits { get; set; }
    public int BenefitVisits { get; set; }
}
