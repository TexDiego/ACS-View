using ACS_View.Application.DTOs;

internal static class VisitRecordGroupingTests
{
    public static void Run()
    {
        var day = new DateTime(2026, 9, 30);
        var morning = new VisitRecordDto { Id = 1, PatientName = "Ana", VisitDate = day.AddHours(9) };
        var afternoon = new VisitRecordDto { Id = 2, PatientName = "Bruno", VisitDate = day.AddHours(15) };
        var previous = new VisitRecordDto { Id = 3, PatientName = "Ana", VisitDate = day.AddDays(-1).AddHours(10) };
        var otherFamily = new VisitRecordDto { Id = 4, PatientName = "Carla", VisitDate = day.AddHours(11) };
        var otherHouse = new VisitRecordDto { Id = 5, PatientName = "Daniel", VisitDate = day.AddHours(12) };
        var groups = VisitRecordDayGroupDto.FromFamilies(
        [
            new() { HouseId = 1, FamilyId = 10, FamilyName = "Família Silva", Visits = [morning, previous] },
            new() { HouseId = 1, FamilyId = 10, FamilyName = "Silva", Visits = [afternoon] },
            new() { HouseId = 1, FamilyId = 11, FamilyName = "Família Silva", Visits = [otherFamily] },
            new() { HouseId = 2, FamilyId = 10, FamilyName = "Família Silva", Visits = [otherHouse] }
        ]);

        Check(groups.Count == 2 && groups[0].Date == day && groups[1].Date == day.AddDays(-1), "Datas devem ignorar horário e aparecer da mais recente para a mais antiga.");
        Check(groups[0].Families.Count == 3, "Famílias distintas, mesmo com nomes iguais, devem ficar separadas por residência e identificador.");
        var family = groups[0].Families.Single(item => item.HouseId == 1 && item.FamilyId == 10);
        Check(family.Visits.Select(item => item.Id).SequenceEqual([2, 1]), "Todos os indivíduos da família no dia devem aparecer, ordenados pelo horário mais recente.");
        Check(groups[1].Families.Single().Visits.Single().Id == 3, "Visitas da mesma pessoa em outro dia devem ficar no dia correspondente.");
        Check(groups.Sum(item => item.VisitCount) == 5, "O agrupamento deve preservar todas as visitas e a contagem.");
        Check(VisitRecordDayGroupDto.FromFamilies([]).Count == 0, "Lista sem registros deve continuar vazia.");
        Console.WriteLine("Visit record grouping tests passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
