using ACS_View.Domain.Enums;

namespace ACS_View.Application.DTOs;

public readonly record struct VaccineScheduleTotals(int Applied, int Pending, int Late)
{
    public static VaccineScheduleTotals From(IEnumerable<PatientVaccineDoseDto> doses)
    {
        var applied = 0;
        var pending = 0;
        var late = 0;

        foreach (var dose in doses)
        {
            switch (dose.Status)
            {
                case VaccineStatus.Applied:
                case VaccineStatus.AppliedLate:
                    applied++;
                    break;
                case VaccineStatus.Due:
                    pending++;
                    break;
                case VaccineStatus.Late:
                    late++;
                    break;
            }
        }

        return new(applied, pending, late);
    }
}
