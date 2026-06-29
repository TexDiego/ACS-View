using ACS_View.Application.DTOs;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.ViewModels;

public sealed class VaccineSchedulePresentation
{
    private readonly IReadOnlyDictionary<string, bool> _appliedDoses;

    public VaccineSchedulePresentation(PatientVaccineScheduleDto schedule)
    {
        PatientId = schedule.PatientId;
        BirthDate = schedule.BirthDate;
        IsPregnant = schedule.IsPregnant;
        _appliedDoses = schedule.AppliedDoses;
    }

    public int PatientId { get; }
    public DateTime BirthDate { get; }
    public bool IsPregnant { get; }

    public bool BCG_Infantil => GetVaccineStatus(VaccineDoseKeys.BcgInfantil);
    public bool HepatitisBAoNascer_Infantil => GetVaccineStatus(VaccineDoseKeys.HepatiteBNascimento);
    public bool Penta1_Infantil => GetVaccineStatus(VaccineDoseKeys.Penta1);
    public bool VIP1_Infantil => GetVaccineStatus(VaccineDoseKeys.Vip1);
    public bool Pneumo10_1_Infantil => GetVaccineStatus(VaccineDoseKeys.Pneumo10_1);
    public bool VRH1_Infantil => GetVaccineStatus(VaccineDoseKeys.Vrh1);
    public bool MeningoC1_Infantil => GetVaccineStatus(VaccineDoseKeys.MeningoC1);
    public bool Penta2_Infantil => GetVaccineStatus(VaccineDoseKeys.Penta2);
    public bool VIP2_Infantil => GetVaccineStatus(VaccineDoseKeys.Vip2);
    public bool Pneumo10_2_Infantil => GetVaccineStatus(VaccineDoseKeys.Pneumo10_2);
    public bool VRH2_Infantil => GetVaccineStatus(VaccineDoseKeys.Vrh2);
    public bool MeningoC2_Infantil => GetVaccineStatus(VaccineDoseKeys.MeningoC2);
    public bool Penta3_Infantil => GetVaccineStatus(VaccineDoseKeys.Penta3);
    public bool VIP3_Infantil => GetVaccineStatus(VaccineDoseKeys.Vip3);
    public bool Covid1_Infantil => GetVaccineStatus(VaccineDoseKeys.Covid1);
    public bool Covid2_Infantil => GetVaccineStatus(VaccineDoseKeys.Covid2);
    public bool FebreAmarela1_Infantil => GetVaccineStatus(VaccineDoseKeys.FebreAmarela1);
    public bool Pneumo10_3_Infantil => GetVaccineStatus(VaccineDoseKeys.Pneumo10_3);
    public bool MeningoC3_Infantil => GetVaccineStatus(VaccineDoseKeys.MeningoC3);
    public bool TripliceViral_Infantil => GetVaccineStatus(VaccineDoseKeys.TripliceViralInfantil);
    public bool DTP1_Infantil => GetVaccineStatus(VaccineDoseKeys.Dtp1);
    public bool VIP4_Infantil => GetVaccineStatus(VaccineDoseKeys.Vip4);
    public bool HepatiteA_Infantil => GetVaccineStatus(VaccineDoseKeys.HepatiteA);
    public bool TetraViral_Infantil => GetVaccineStatus(VaccineDoseKeys.TetraViral);
    public bool DTP2_Infantil => GetVaccineStatus(VaccineDoseKeys.Dtp2);
    public bool FebreAmarela2_Infantil => GetVaccineStatus(VaccineDoseKeys.FebreAmarela2);
    public bool Varicela_Infantil => GetVaccineStatus(VaccineDoseKeys.Varicela);
    public bool FebreAmarela3_Infantil => GetVaccineStatus(VaccineDoseKeys.FebreAmarela3);
    public bool Pneumo23_Infantil => GetVaccineStatus(VaccineDoseKeys.Pneumo23);
    public bool DT_Infantil => GetVaccineStatus(VaccineDoseKeys.DtInfantil);
    public bool HPV_Infantil => GetVaccineStatus(VaccineDoseKeys.HpvInfantil);

    public bool HepatiteB_Adolescente => GetVaccineStatus(VaccineDoseKeys.HepatiteBAdolescente);
    public bool DT_Adolescente => GetVaccineStatus(VaccineDoseKeys.DtAdolescente);
    public bool FebreAmarela_Adolescente => GetVaccineStatus(VaccineDoseKeys.FebreAmarelaAdolescente);
    public bool TripliceViral_Adolescente => GetVaccineStatus(VaccineDoseKeys.TripliceViralAdolescente);
    public bool HPV_Adolescente => GetVaccineStatus(VaccineDoseKeys.HpvAdolescente);
    public bool ACWY_Adolescente => GetVaccineStatus(VaccineDoseKeys.Acwy);

    public bool HepatiteB_Adulto => GetVaccineStatus(VaccineDoseKeys.HepatiteBAdulto);
    public bool dT_Adulto => GetVaccineStatus(VaccineDoseKeys.DtAdulto);
    public bool FebreAmarela_Adulto => GetVaccineStatus(VaccineDoseKeys.FebreAmarelaAdulto);
    public bool HPV_Adulto => GetVaccineStatus(VaccineDoseKeys.HpvAdulto);
    public bool TripliceViral1_Adulto => GetVaccineStatus(VaccineDoseKeys.TripliceViralAdulto20A29);
    public bool TripliceViral2_Adulto => GetVaccineStatus(VaccineDoseKeys.TripliceViralAdulto30A59);
    public bool dTpa_Adulto => GetVaccineStatus(VaccineDoseKeys.DtpaAdulto);

    public bool HepatiteB_Idoso => GetVaccineStatus(VaccineDoseKeys.HepatiteBIdoso);
    public bool dT_Idoso => GetVaccineStatus(VaccineDoseKeys.DtIdoso);
    public bool FebreAmarela_Idoso => GetVaccineStatus(VaccineDoseKeys.FebreAmarelaIdoso);
    public bool dTpa_Idoso => GetVaccineStatus(VaccineDoseKeys.DtpaIdoso);

    public bool HepatiteB_Gestante => GetVaccineStatus(VaccineDoseKeys.HepatiteBGestante);
    public bool dT_Gestante => GetVaccineStatus(VaccineDoseKeys.DtGestante);
    public bool dTpa_Gestante => GetVaccineStatus(VaccineDoseKeys.DtpaGestante);

    public bool ShowRN => AgeMonths is >= 0 and <= 120;
    public bool Show2Meses => AgeMonths is >= 2 and <= 120;
    public bool Show3Meses => AgeMonths is >= 3 and <= 120;
    public bool Show4Meses => AgeMonths is >= 4 and <= 120;
    public bool Show5Meses => AgeMonths is >= 5 and <= 120;
    public bool Show6Meses => AgeMonths is >= 6 and <= 120;
    public bool Show7Meses => AgeMonths is >= 7 and <= 120;
    public bool Show9Meses => AgeMonths is >= 9 and <= 120;
    public bool Show1Ano => AgeMonths is >= 12 and <= 120;
    public bool Show15Meses => AgeMonths is >= 15 and <= 120;
    public bool Show4Anos => AgeMonths is >= 48 and <= 120;
    public bool Show5Anos => AgeMonths is >= 60 and <= 120;
    public bool Show7Anos => AgeMonths is >= 72 and <= 120
        && !Penta1_Infantil
        && !Penta2_Infantil
        && !Penta3_Infantil
        && !DTP1_Infantil
        && !DTP2_Infantil;
    public bool Show9Anos => AgeMonths is >= 108 and <= 120;

    public bool ShowHB => IsYoung;
    public bool ShowDT => IsYoung;
    public bool ShowFebreAmarela => IsYoung &&
        ((!FebreAmarela1_Infantil && !FebreAmarela2_Infantil && !FebreAmarela3_Infantil) ||
         !FebreAmarela1_Infantil ||
         !FebreAmarela2_Infantil);
    public bool ShowTripliceViral => IsYoung;
    public bool ShowHPV => IsYoung;
    public bool ShowACWY => IsYoung;

    public bool ShowHBAdulto => IsAdult;
    public bool ShowdTAdulto => IsAdult;
    public bool ShowFebreAmarelaAdulto => IsAdult;
    public bool ShowHPVAdulto => IsAdult;
    public bool ShowTripliceViral1Adulto => IsAdult;
    public bool ShowTripliceViral2Adulto => IsAdult;
    public bool ShowdTpaAdulto => IsAdult;

    public bool ShowHBIdoso => IsElderly;
    public bool ShowdTIdoso => IsElderly;
    public bool ShowFebreAmarelaIdoso => IsElderly;
    public bool ShowdTpaIdoso => IsElderly;

    public bool ShowHBGestante => IsPregnant;
    public bool ShowdTGestante => IsPregnant;
    public bool ShowdTpaGestante => IsPregnant;

    public bool IsChild => AgeMonths <= 120;
    public bool IsYoung => AgeMonths is > 120 and <= 180;
    public bool IsAdult => AgeMonths is > 180 and < 720;
    public bool IsElderly => AgeMonths >= 720;

    public bool GetVaccineStatus(string doseKey)
    {
        return _appliedDoses.TryGetValue(doseKey, out var isApplied) && isApplied;
    }

    private int AgeMonths => GetMonth(BirthDate);

    private static int GetMonth(DateTime birthDate)
    {
        var today = DateTime.Today;
        var months = (today.Year - birthDate.Year) * 12 + today.Month - birthDate.Month;

        if (today.Day < birthDate.Day)
        {
            months--;
        }

        return Math.Max(months, 0);
    }
}
