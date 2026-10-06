namespace ACS_View.Application.DTOs;

public sealed class VisitRecordDayGroupDto
{
    public DateTime Date { get; init; }
    public List<VisitRecordFamilyGroupDto> Families { get; init; } = [];
    public int VisitCount => Families.Sum(family => family.Visits.Count);

    public static List<VisitRecordDayGroupDto> FromFamilies(IEnumerable<VisitRecordFamilyGroupDto> groups)
    {
        return groups
            .SelectMany(family => family.Visits.Select(visit => new { Family = family, Visit = visit }))
            .GroupBy(item => item.Visit.VisitDate.Date)
            .OrderByDescending(day => day.Key)
            .Select(day => new VisitRecordDayGroupDto
            {
                Date = day.Key,
                Families = day
                    .GroupBy(item => new { item.Family.HouseId, item.Family.FamilyId })
                    .Select(family => new VisitRecordFamilyGroupDto
                    {
                        HouseId = family.Key.HouseId,
                        FamilyId = family.Key.FamilyId,
                        FamilyName = family.First().Family.FamilyName,
                        Visits = family.Select(item => item.Visit)
                            .OrderByDescending(visit => visit.VisitDate)
                            .ThenBy(visit => visit.PatientName, StringComparer.CurrentCultureIgnoreCase)
                            .ThenBy(visit => visit.Id)
                            .ToList()
                    })
                    .OrderBy(family => family.FamilyName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(family => family.HouseId)
                    .ThenBy(family => family.FamilyId)
                    .ToList()
            })
            .ToList();
    }
}
