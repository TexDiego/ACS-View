using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class VaccineDoseCatalog
{
    private static readonly string BcgDescription = "Tuberculose";
    private static readonly string HepatitisBDescription = "Hepatite B";
    private static readonly string PentaDescription = "Difteria, Tétano, Coqueluche, Hepatite B e infecções causadas pelo Haemophilus influenzae B";
    private static readonly string VipDescription = "Poliomelite";
    private static readonly string Pneumo10Description = "Infecções invasivas (como meningite e pneumonia) e otite média média aguda, causadas pelos 10 sorotipos de Streptococus pneumoniae";
    private static readonly string VrhDescription = "Diarreia por rotavírus (Gastroenterites)";
    private static readonly string MeningoCDescription = "Doença invasiva causada pela Neisseria meningitidis do sorogrupo C";
    private static readonly string CovidDescription = "Proteção contra as formas graves e complicações pela covid-19";
    private static readonly string YellowFeverDescription = "Febre amarela";
    private static readonly string TripliceViralDescription = "Sarampo, caxumba e rubéola";
    private static readonly string DtpDescription = "Difteria, tétano e coqueluche";
    private static readonly string HepatitisADescription = "Hepatite A";
    private static readonly string TetraViralDescription = "Sarampo, caxumba, rubéola e varicela";
    private static readonly string VaricelaDescription = "Varicela";
    private static readonly string Pneumo23Description = "Para a proteção contra infecções invasivas pela bactéria pneumococo";
    private static readonly string DtDescription = "Difteria e Tétano";
    private static readonly string HpvDescription = "Proteção contra Papilomavírus Humano 6, 11, 16 e 18";
    private static readonly string AcwyDescription = "Proteção contra as meningites causadas pelos sorogrupos A, C, W e Y da Neisseria meningitidis";
    private static readonly string DtpaDescription = "Proteção contra Difteria, Tétano e Coqueluche em adultos";

    public static readonly IReadOnlyList<VaccineDoseDefinition> Definitions =
    [
        Dose(VaccineDoseKeys.BcgInfantil, "BCG", "Única", VaccineSectionType.Child, 0, 1, "Ao nascer", BcgDescription),
        Dose(VaccineDoseKeys.HepatiteBNascimento, "Hepatite B", "Única", VaccineSectionType.Child, 0, 1, "Ao nascer", HepatitisBDescription),
        Dose(VaccineDoseKeys.Penta1, "Penta", "1ª dose", VaccineSectionType.Child, 2, 3, "2 meses", PentaDescription),
        Dose(VaccineDoseKeys.Vip1, "VIP", "1ª dose", VaccineSectionType.Child, 2, 3, "2 meses", VipDescription),
        Dose(VaccineDoseKeys.Pneumo10_1, "Pneumo 10", "1ª dose", VaccineSectionType.Child, 2, 3, "2 meses", Pneumo10Description),
        Dose(VaccineDoseKeys.Vrh1, "VRH", "1ª dose", VaccineSectionType.Child, 2, 3, "2 meses", VrhDescription),
        Dose(VaccineDoseKeys.MeningoC1, "Meningo C", "1ª dose", VaccineSectionType.Child, 3, 4, "3 meses", MeningoCDescription),
        Dose(VaccineDoseKeys.Penta2, "Penta", "2ª dose", VaccineSectionType.Child, 4, 5, "4 meses", PentaDescription),
        Dose(VaccineDoseKeys.Vip2, "VIP", "2ª dose", VaccineSectionType.Child, 4, 5, "4 meses", VipDescription),
        Dose(VaccineDoseKeys.Pneumo10_2, "Pneumo 10", "2ª dose", VaccineSectionType.Child, 4, 5, "4 meses", Pneumo10Description),
        Dose(VaccineDoseKeys.Vrh2, "VRH", "2ª dose", VaccineSectionType.Child, 4, 5, "4 meses", VrhDescription),
        Dose(VaccineDoseKeys.MeningoC2, "Meningo C", "2ª dose", VaccineSectionType.Child, 5, 6, "5 meses", MeningoCDescription),
        Dose(VaccineDoseKeys.Penta3, "Penta", "3ª dose", VaccineSectionType.Child, 6, 7, "6 meses", PentaDescription),
        Dose(VaccineDoseKeys.Vip3, "VIP", "3ª dose", VaccineSectionType.Child, 6, 7, "6 meses", VipDescription),
        Dose(VaccineDoseKeys.Covid1, "Covid 19", "1ª dose", VaccineSectionType.Child, 6, 7, "6 meses", CovidDescription),
        Dose(VaccineDoseKeys.Covid2, "Covid 19", "2ª dose", VaccineSectionType.Child, 7, 8, "7 meses", CovidDescription),
        Dose(VaccineDoseKeys.FebreAmarela1, "Febre Amarela", "1ª dose", VaccineSectionType.Child, 9, 11, "9 meses", YellowFeverDescription),
        Dose(VaccineDoseKeys.Pneumo10_3, "Pneumo 10", "Reforço", VaccineSectionType.Child, 12, 14, "1 ano", Pneumo10Description),
        Dose(VaccineDoseKeys.MeningoC3, "Meningo C", "Reforço", VaccineSectionType.Child, 12, 14, "1 ano", MeningoCDescription),
        Dose(VaccineDoseKeys.TripliceViralInfantil, "Tríplice Viral", "1ª dose", VaccineSectionType.Child, 12, 14, "1 ano", TripliceViralDescription),
        Dose(VaccineDoseKeys.Dtp1, "DTP", "1º reforço", VaccineSectionType.Child, 15, 23, "15 meses", DtpDescription),
        Dose(VaccineDoseKeys.Vip4, "VIP", "Reforço", VaccineSectionType.Child, 15, 23, "15 meses", VipDescription),
        Dose(VaccineDoseKeys.HepatiteA, "Hepatite A", "Uma dose", VaccineSectionType.Child, 15, 23, "15 meses", HepatitisADescription),
        Dose(VaccineDoseKeys.TetraViral, "Tetra Viral", "Uma dose", VaccineSectionType.Child, 15, 23, "15 meses", TetraViralDescription),
        Dose(VaccineDoseKeys.Dtp2, "DTP", "2ª reforço", VaccineSectionType.Child, 48, 59, "4 anos", DtpDescription),
        Dose(VaccineDoseKeys.FebreAmarela2, "Febre Amarela", "Reforço", VaccineSectionType.Child, 48, 59, "4 anos", YellowFeverDescription),
        Dose(VaccineDoseKeys.Varicela, "Varicela", "Uma dose", VaccineSectionType.Child, 48, 59, "4 anos", VaricelaDescription),
        Dose(VaccineDoseKeys.FebreAmarela3, "Febre Amarela", "Uma dose", VaccineSectionType.Child, 60, 71, "5 anos", YellowFeverDescription),
        Dose(VaccineDoseKeys.Pneumo23, "Pneumo 23", "Duas doses", VaccineSectionType.Child, 60, 71, "5 anos", Pneumo23Description),
        Dose(VaccineDoseKeys.DtInfantil, "DT", "Três doses", VaccineSectionType.Child, 72, 107, "7 anos", DtDescription),
        Dose(VaccineDoseKeys.HpvInfantil, "HPV", "Uma dose", VaccineSectionType.Child, 108, 120, "9 anos", HpvDescription),

        Dose(VaccineDoseKeys.HepatiteBAdolescente, "Hepatite B", "Única", VaccineSectionType.Teen, 121, 180, "Qualquer", HepatitisBDescription),
        Dose(VaccineDoseKeys.DtAdolescente, "dT", "Completar", VaccineSectionType.Teen, 121, 180, "Qualquer", DtDescription),
        Dose(VaccineDoseKeys.FebreAmarelaAdolescente, "Febre Amarela", "Completar", VaccineSectionType.Teen, 121, 180, "Qualquer", YellowFeverDescription),
        Dose(VaccineDoseKeys.TripliceViralAdolescente, "Tríplice Viral", "Completar", VaccineSectionType.Teen, 121, 180, "Qualquer", TripliceViralDescription),
        Dose(VaccineDoseKeys.HpvAdolescente, "HPV", "Dose única", VaccineSectionType.Teen, 121, 180, "11-14 anos", HpvDescription),
        Dose(VaccineDoseKeys.Acwy, "ACWY", "Uma dose", VaccineSectionType.Teen, 121, 180, "11-14 anos", AcwyDescription),

        Dose(VaccineDoseKeys.HepatiteBAdulto, "Hepatite B", "Completar", VaccineSectionType.Adult, 181, 719, "Adulto", HepatitisBDescription),
        Dose(VaccineDoseKeys.DtAdulto, "dT", "Completar", VaccineSectionType.Adult, 181, 719, "Adulto", DtDescription),
        Dose(VaccineDoseKeys.FebreAmarelaAdulto, "Febre Amarela", "Completar", VaccineSectionType.Adult, 181, 719, "Adulto", YellowFeverDescription),
        Dose(VaccineDoseKeys.HpvAdulto, "HPV", "Completar", VaccineSectionType.Adult, 181, 719, "9-45 anos", HpvDescription),
        Dose(VaccineDoseKeys.TripliceViralAdulto20A29, "Tríplice Viral", "Uma dose", VaccineSectionType.Adult, 240, 359, "20-29 anos", TripliceViralDescription),
        Dose(VaccineDoseKeys.TripliceViralAdulto30A59, "Tríplice Viral", "Uma dose", VaccineSectionType.Adult, 360, 719, "30-59 anos", TripliceViralDescription),
        Dose(VaccineDoseKeys.DtpaAdulto, "dTpa", "Uma dose", VaccineSectionType.Adult, 216, null, "18 anos +", DtpaDescription),

        Dose(VaccineDoseKeys.HepatiteBIdoso, "Hepatite B", "Completar", VaccineSectionType.Elderly, 720, null, "60 anos +", HepatitisBDescription),
        Dose(VaccineDoseKeys.DtIdoso, "dT", "Completar", VaccineSectionType.Elderly, 720, null, "60 anos +", DtDescription),
        Dose(VaccineDoseKeys.FebreAmarelaIdoso, "Febre Amarela", "Completar", VaccineSectionType.Elderly, 720, null, "60 anos +", YellowFeverDescription),
        Dose(VaccineDoseKeys.DtpaIdoso, "DTPA", "Completar", VaccineSectionType.Elderly, 720, null, "60 anos +", DtpaDescription),

        Dose(VaccineDoseKeys.HepatiteBGestante, "Hepatite B", "Completar", VaccineSectionType.Pregnant, null, null, "Qualquer", HepatitisBDescription, requiresPregnancy: true),
        Dose(VaccineDoseKeys.DtGestante, "dT", "Completar", VaccineSectionType.Pregnant, null, null, "Qualquer", DtDescription, requiresPregnancy: true),
        Dose(VaccineDoseKeys.DtpaGestante, "dTpa", "Completar", VaccineSectionType.Pregnant, null, null, "20ª semana", DtpaDescription, requiresPregnancy: true),
    ];

    public static readonly IReadOnlyDictionary<string, string> LegacyBooleanColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["BCG_Infantil"] = VaccineDoseKeys.BcgInfantil,
        ["HepatitisBAoNascer_Infantil"] = VaccineDoseKeys.HepatiteBNascimento,
        ["Penta1_Infantil"] = VaccineDoseKeys.Penta1,
        ["VIP1_Infantil"] = VaccineDoseKeys.Vip1,
        ["Pneumo10_1_Infantil"] = VaccineDoseKeys.Pneumo10_1,
        ["VRH1_Infantil"] = VaccineDoseKeys.Vrh1,
        ["MeningoC1_Infantil"] = VaccineDoseKeys.MeningoC1,
        ["Penta2_Infantil"] = VaccineDoseKeys.Penta2,
        ["VIP2_Infantil"] = VaccineDoseKeys.Vip2,
        ["Pneumo10_2_Infantil"] = VaccineDoseKeys.Pneumo10_2,
        ["VRH2_Infantil"] = VaccineDoseKeys.Vrh2,
        ["MeningoC2_Infantil"] = VaccineDoseKeys.MeningoC2,
        ["Penta3_Infantil"] = VaccineDoseKeys.Penta3,
        ["VIP3_Infantil"] = VaccineDoseKeys.Vip3,
        ["Covid1_Infantil"] = VaccineDoseKeys.Covid1,
        ["Covid2_Infantil"] = VaccineDoseKeys.Covid2,
        ["FebreAmarela1_Infantil"] = VaccineDoseKeys.FebreAmarela1,
        ["Pneumo10_3_Infantil"] = VaccineDoseKeys.Pneumo10_3,
        ["MeningoC3_Infantil"] = VaccineDoseKeys.MeningoC3,
        ["TripliceViral_Infantil"] = VaccineDoseKeys.TripliceViralInfantil,
        ["DTP1_Infantil"] = VaccineDoseKeys.Dtp1,
        ["VIP4_Infantil"] = VaccineDoseKeys.Vip4,
        ["HepatiteA_Infantil"] = VaccineDoseKeys.HepatiteA,
        ["TetraViral_Infantil"] = VaccineDoseKeys.TetraViral,
        ["DTP2_Infantil"] = VaccineDoseKeys.Dtp2,
        ["FebreAmarela2_Infantil"] = VaccineDoseKeys.FebreAmarela2,
        ["Varicela_Infantil"] = VaccineDoseKeys.Varicela,
        ["FebreAmarela3_Infantil"] = VaccineDoseKeys.FebreAmarela3,
        ["Pneumo23_Infantil"] = VaccineDoseKeys.Pneumo23,
        ["DT_Infantil"] = VaccineDoseKeys.DtInfantil,
        ["HPV_Infantil"] = VaccineDoseKeys.HpvInfantil,
        ["HepatiteB_Adolescente"] = VaccineDoseKeys.HepatiteBAdolescente,
        ["DT_Adolescente"] = VaccineDoseKeys.DtAdolescente,
        ["FebreAmarela_Adolescente"] = VaccineDoseKeys.FebreAmarelaAdolescente,
        ["TripliceViral_Adolescente"] = VaccineDoseKeys.TripliceViralAdolescente,
        ["HPV_Adolescente"] = VaccineDoseKeys.HpvAdolescente,
        ["ACWY_Adolescente"] = VaccineDoseKeys.Acwy,
        ["HepatiteB_Adulto"] = VaccineDoseKeys.HepatiteBAdulto,
        ["dT_Adulto"] = VaccineDoseKeys.DtAdulto,
        ["FebreAmarela_Adulto"] = VaccineDoseKeys.FebreAmarelaAdulto,
        ["HPV_Adulto"] = VaccineDoseKeys.HpvAdulto,
        ["TripliceViral1_Adulto"] = VaccineDoseKeys.TripliceViralAdulto20A29,
        ["TripliceViral2_Adulto"] = VaccineDoseKeys.TripliceViralAdulto30A59,
        ["dTpa_Adulto"] = VaccineDoseKeys.DtpaAdulto,
        ["HepatiteB_Idoso"] = VaccineDoseKeys.HepatiteBIdoso,
        ["dT_Idoso"] = VaccineDoseKeys.DtIdoso,
        ["FebreAmarela_Idoso"] = VaccineDoseKeys.FebreAmarelaIdoso,
        ["dTpa_Idoso"] = VaccineDoseKeys.DtpaIdoso,
        ["HepatiteB_Gestante"] = VaccineDoseKeys.HepatiteBGestante,
        ["dT_Gestante"] = VaccineDoseKeys.DtGestante,
        ["dTpa_Gestante"] = VaccineDoseKeys.DtpaGestante
    };

    private static readonly IReadOnlyDictionary<string, VaccineDoseDefinition> ByKey =
        Definitions.ToDictionary(definition => definition.DoseKey, StringComparer.OrdinalIgnoreCase);

    public static VaccineDoseDefinition? GetDefinition(string doseKey)
    {
        return ByKey.TryGetValue(doseKey, out var definition) ? definition : null;
    }

    public static IEnumerable<VaccineDoseDefinition> GetApplicableDefinitions(int ageMonths, bool isPregnant)
    {
        return Definitions.Where(definition =>
            (!definition.RequiresPregnancy || isPregnant) &&
            (!definition.MinAgeMonths.HasValue || ageMonths >= definition.MinAgeMonths.Value) &&
            (!definition.MaxAgeMonths.HasValue || ageMonths <= definition.MaxAgeMonths.Value));
    }

    public static IEnumerable<VaccineDoseDefinition> GetExpectedDefinitions(int ageMonths, bool isPregnant)
    {
        var definitions = ageMonths <= 120
            ? Definitions.Where(definition => definition.Section == VaccineSectionType.Child)
            : GetApplicableDefinitions(ageMonths, isPregnant);

        if (isPregnant && ageMonths <= 120)
        {
            definitions = definitions.Concat(Definitions.Where(definition => definition.RequiresPregnancy));
        }

        return definitions
            .Where(definition => !definition.RequiresPregnancy || isPregnant)
            .OrderBy(definition => definition.MinAgeMonths ?? int.MaxValue)
            .ThenBy(definition => definition.MaxAgeMonths ?? int.MaxValue)
            .ThenBy(definition => definition.VaccineName)
            .ThenBy(definition => definition.DoseLabel);
    }

    public static IEnumerable<VaccineDoseDefinition> GetRequiredChildDefinitions()
    {
        return Definitions.Where(definition => definition.IsRequiredForChildMetric);
    }

    private static VaccineDoseDefinition Dose(
        string doseKey,
        string vaccineName,
        string doseLabel,
        VaccineSectionType section,
        int? minAgeMonths,
        int? maxAgeMonths,
        string ageLabel,
        string diseaseDescription,
        bool requiresPregnancy = false)
    {
        return new VaccineDoseDefinition(
            doseKey,
            vaccineName,
            doseLabel,
            section,
            minAgeMonths,
            maxAgeMonths,
            section == VaccineSectionType.Child,
            requiresPregnancy,
            ageLabel,
            diseaseDescription);
    }
}
