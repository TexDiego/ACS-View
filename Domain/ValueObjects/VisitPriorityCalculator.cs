using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class VisitPriorityCalculator
{
    public static VisitPriorityResult Calculate(IEnumerable<VisitCareLineType> careLines)
    {
        var careLineSet = careLines.ToHashSet();
        var hasAgePriority = careLineSet.Contains(VisitCareLineType.Child) ||
                             careLineSet.Contains(VisitCareLineType.Elderly);
        var hasBenefit = careLineSet.Contains(VisitCareLineType.BolsaFamilia) ||
                         careLineSet.Contains(VisitCareLineType.Bpc);

        if (hasAgePriority && hasBenefit)
        {
            return new VisitPriorityResult(2.5m, "Prioridade 2.5: criança/idoso e beneficiário Bolsa Família");
        }

        if (hasBenefit)
        {
            return new VisitPriorityResult(1.3m, "Prioridade 1.3: beneficiário Bolsa Família");
        }

        if (hasAgePriority)
        {
            return new VisitPriorityResult(1.2m, "Prioridade 1.2: criança ou idoso");
        }

        return new VisitPriorityResult(1.0m, "Prioridade 1.0: sem critério adicional de vulnerabilidade");
    }
}

public sealed record VisitPriorityResult(decimal Factor, string Label);
