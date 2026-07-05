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
            return new VisitPriorityResult(2.5m, "Prioridade 2.5: crianca/idoso e beneficiario Bolsa Familia/BPC");
        }

        if (hasBenefit)
        {
            return new VisitPriorityResult(1.3m, "Prioridade 1.3: beneficiario Bolsa Familia/BPC");
        }

        if (hasAgePriority)
        {
            return new VisitPriorityResult(1.2m, "Prioridade 1.2: crianca ou idoso");
        }

        return new VisitPriorityResult(1.0m, "Prioridade 1.0: sem criterio adicional de vulnerabilidade");
    }
}

public sealed record VisitPriorityResult(decimal Factor, string Label);
