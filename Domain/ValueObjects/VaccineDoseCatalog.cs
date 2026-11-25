using ACS_View.Domain.Enums;

namespace ACS_View.Domain.ValueObjects;

public static class VaccineDoseCatalog
{
    public const string Source = "Manual do Profissional de Imunização 2026, Kirky Dejane, edição conforme IN de 08/10/2026";
    public const string ScheduleNotice = "Calendário 2026 · Confira o histórico antes de registrar. Avaliação, doses sazonais e grupos especiais não entram no lote de atrasadas.";
    private static readonly string BcgDescription = "Tuberculose";
    private static readonly string HepatitisBDescription = "Hepatite B";
    private static readonly string PentaDescription = "Difteria, Tétano, Coqueluche, Hepatite B e infecções causadas pelo Haemophilus influenzae B";
    private static readonly string VipDescription = "Poliomielite";
    private static readonly string Pneumo10Description = "Infecções invasivas (como meningite e pneumonia) e otite média aguda, causadas pelos 10 sorotipos de Streptococcus pneumoniae";
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

    public static readonly IReadOnlyList<VaccineDoseDefinition> Definitions = Update2026(
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
    ]);

    // Existing keys retain their original product and records. Replacements receive new keys.
    private static IReadOnlyList<VaccineDoseDefinition> Update2026(VaccineDoseDefinition[] legacy)
    {
        var retired = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            VaccineDoseKeys.Pneumo10_1, VaccineDoseKeys.Pneumo10_3, VaccineDoseKeys.MeningoC3,
            VaccineDoseKeys.FebreAmarela3, VaccineDoseKeys.Pneumo23,
            VaccineDoseKeys.HpvAdolescente
        };
        var result = legacy.Select(d =>
        {
            var updated = d with
            {
                IsRoutine = !retired.Contains(d.DoseKey),
                IsRequiredForChildMetric = d.Section == VaccineSectionType.Child && !retired.Contains(d.DoseKey),
                Guidance = "Conferir doses documentadas e intervalos. Esquema atrasado não deve ser reiniciado. Manual 2026, pp. 82–104."
            };
            if (retired.Contains(d.DoseKey))
                return updated with { IsRequiredForChildMetric = false, Guidance = "Registro do calendário anterior ou alternativa. Preservar a aplicação original; conferir o esquema atual antes de indicar outra dose." };

            var limit = d.DoseKey switch
            {
                VaccineDoseKeys.BcgInfantil => 60,
                VaccineDoseKeys.HepatiteBNascimento => 1,
                VaccineDoseKeys.Penta1 or VaccineDoseKeys.Penta2 or VaccineDoseKeys.Penta3 or VaccineDoseKeys.Dtp1 or VaccineDoseKeys.Dtp2 or VaccineDoseKeys.Varicela => 84,
                VaccineDoseKeys.Vip1 or VaccineDoseKeys.Vip2 or VaccineDoseKeys.Vip3 or VaccineDoseKeys.Vip4 or VaccineDoseKeys.Pneumo10_2 or VaccineDoseKeys.HepatiteA or VaccineDoseKeys.Covid1 or VaccineDoseKeys.Covid2 => 60,
                VaccineDoseKeys.Vrh1 or VaccineDoseKeys.MeningoC1 or VaccineDoseKeys.MeningoC2 => 12,
                VaccineDoseKeys.Vrh2 => 24,
                VaccineDoseKeys.HpvInfantil or VaccineDoseKeys.Acwy => 180,
                _ => (int?)null
            };
            updated = updated with { AdministrationAgeLimitMonthsExclusive = limit };
            if (d.Section == VaccineSectionType.Teen)
                updated = updated with { MinAgeMonths = 120, MaxAgeMonths = 239, AgeLabel = "10 a 19 anos · conferir histórico" };
            if (d.Section == VaccineSectionType.Adult && d.MinAgeMonths == 181)
                updated = updated with { MinAgeMonths = 240 };

            return d.DoseKey switch
            {
                VaccineDoseKeys.BcgInfantil => updated with { Guidance = "Dose ao nascer; rotina até 4 anos, 11 meses e 29 dias. Ausência de cicatriz não indica revacinação. Manual, pp. 17–20 e 85." },
                VaccineDoseKeys.HepatiteBNascimento => updated with { DoseLabel = "Dose ao nascer", Guidance = "Ideal nas primeiras 12 horas; dose do RN até 1 mês de vida. Depois, seguir esquema com penta. Manual, pp. 21–24 e 85." },
                VaccineDoseKeys.Vip1 or VaccineDoseKeys.Vip2 or VaccineDoseKeys.Vip3 or VaccineDoseKeys.Vip4 => updated with { DiseaseDescription = "Poliomielite", DoseLabel = d.DoseKey == VaccineDoseKeys.Vip4 ? "1º reforço" : d.DoseLabel, Guidance = "VIP aos 2, 4 e 6 meses; reforços aos 15 meses e 4 anos. Mínimo de 6 meses antes de cada reforço. Rotina até 4 anos, 11 meses e 29 dias. Manual, pp. 27–29 e 82–85." },
                VaccineDoseKeys.Vrh1 or VaccineDoseKeys.Vrh2 => updated with { VaccineName = "Rotavírus", Guidance = "1ª dose aos 2 meses (até 11 meses e 29 dias); 2ª aos 4 meses (até 23 meses e 29 dias). Mínimo de 30 dias. Se perdeu o limite da 1ª, não iniciar. Cuspiu ou vomitou: não repetir. Manual, pp. 30–32." },
                VaccineDoseKeys.Pneumo10_2 => updated with { RequiresAssessment = true, IsRequiredForChildMetric = false, Guidance = "Transição: VPC 20 aos 2 meses, VPC 10 aos 4 e VPC 20 aos 12. Sem VPC 10, usar VPC 20. Resgate depende da idade e do histórico, sem reiniciar. Manual, pp. 33–38." },
                VaccineDoseKeys.MeningoC1 or VaccineDoseKeys.MeningoC2 => updated with { Guidance = "Men C aos 3 e 5 meses, até 11 meses e 29 dias. Após essa idade, avaliar Men ACWY. Manual, pp. 39–41 e 85." },
                VaccineDoseKeys.Covid1 or VaccineDoseKeys.Covid2 => updated with { VaccineName = "Covid-19 · Pfizer", Guidance = "Esquema infantil: 6, 7 e 9 meses. Mínimo de 4 semanas entre D1 e D2 e 8 semanas entre D2 e D3. Contar doses anteriores e produto utilizado; rotina até 4 anos, 11 meses e 29 dias. Manual, pp. 45–48." },
                VaccineDoseKeys.Varicela => updated with { DoseLabel = "2ª dose", AvoidDuringPregnancy = true },
                VaccineDoseKeys.TetraViral => updated with { DoseLabel = "Alternativa à tríplice viral + varicela", RequiresAssessment = true, IsRequiredForChildMetric = false, AdministrationAgeLimitMonthsExclusive = 60, AvoidDuringPregnancy = true, Guidance = "Não é rotina aos 15 meses: usar na falta de varicela monovalente ou alergia à proteína do leite, conforme avaliação. Considerar componentes já registrados para não duplicar. Manual, pp. 57–58 e 84." },
                VaccineDoseKeys.HpvInfantil => updated with { VaccineName = "HPV4", MinAgeMonths = 108, MaxAgeMonths = 179, AgeLabel = "9 a 14 anos", IsRequiredForChildMetric = false, AvoidDuringPregnancy = true, Guidance = "Rotina: dose única dos 9 aos 14 anos, 11 meses e 29 dias. Resgate de 15 a 19 anos conforme estratégia estadual. Grupos especiais têm esquemas próprios. Não aplicar na gestação. Manual, pp. 66–68 e 94–95." },
                VaccineDoseKeys.Acwy => updated with { MinAgeMonths = 132, MaxAgeMonths = 179, AgeLabel = "11 a 14 anos", Guidance = "Dose ou reforço dos 11 aos 14 anos, 11 meses e 29 dias, mesmo com Men C na infância. Manual, pp. 52–54 e 94." },
                VaccineDoseKeys.TripliceViralAdulto20A29 => updated with { DoseLabel = "Completar 2 doses", RequiresAssessment = true, AvoidDuringPregnancy = true, Guidance = "20–29 anos: comprovar 2 doses; intervalo recomendado 30 dias (mínimo 15). Trabalhadores da saúde: 2 doses em qualquer idade. Não aplicar na gestação. Manual, pp. 97–98." },
                VaccineDoseKeys.TripliceViralAdulto30A59 => updated with { DoseLabel = "Comprovar 1 dose", RequiresAssessment = true, AvoidDuringPregnancy = true },
                VaccineDoseKeys.TripliceViralAdolescente => updated with { DoseLabel = "Completar 2 doses", RequiresAssessment = true, AvoidDuringPregnancy = true },
                VaccineDoseKeys.TripliceViralInfantil => updated with { AvoidDuringPregnancy = true },
                VaccineDoseKeys.FebreAmarela1 or VaccineDoseKeys.FebreAmarela2 => updated with { AvoidDuringPregnancy = true, Guidance = "9 meses e reforço aos 4 anos. Resgate depende da idade da primeira dose; não indicar automaticamente uma terceira dose aos 5 anos. Manual, pp. 48–51 e 95." },
                VaccineDoseKeys.FebreAmarelaAdolescente or VaccineDoseKeys.FebreAmarelaAdulto or VaccineDoseKeys.FebreAmarelaIdoso => updated with { RequiresAssessment = true, AvoidDuringPregnancy = true, Guidance = "Sem dose: avaliar uma dose. Uma dose antes dos 5 anos: reforço. Uma a partir dos 5 anos ou duas antes: completo. Idoso: avaliar risco-benefício e exposição. Gestante: apenas situações de alto risco avaliadas. Manual, pp. 95, 97 e 101." },
                VaccineDoseKeys.HpvAdulto => updated with { MinAgeMonths = 240, RequiresAssessment = true, AvoidDuringPregnancy = true, Guidance = "Não é rotina universal do adulto. Imunodeprimidos até 45 anos; PrEP 15–45; vítimas de abuso sexual até 45; mulheres tratadas de NIC 2, NIC 3 ou AIS em qualquer idade. Em geral 3 doses (0, 2, 6 meses); confirmar indicação. Manual, pp. 66–68 e 98." },
                VaccineDoseKeys.DtpaAdulto or VaccineDoseKeys.DtpaIdoso => updated with { MinAgeMonths = d.Section == VaccineSectionType.Adult ? 240 : 720, MaxAgeMonths = d.Section == VaccineSectionType.Adult ? 719 : null, RequiresAssessment = true, RepeatIntervalMonths = 120, Guidance = "Indicação ocupacional para trabalhadores da saúde: uma dose e reforços decenais com dTpa. Conferir histórico; não é rotina universal. Manual, pp. 74–77 e 98." },
                VaccineDoseKeys.DtpaGestante => updated with { PregnancyWeek = 20, PerPregnancy = true, DoseLabel = "Uma dose em cada gestação", Guidance = "A partir da 20ª semana em toda gestação. Conta como uma das 3 doses com componente tetânico; completar dT conforme histórico. Manual, pp. 74–77 e 103–104." },
                VaccineDoseKeys.HepatiteBAdolescente or VaccineDoseKeys.HepatiteBAdulto or VaccineDoseKeys.HepatiteBIdoso or VaccineDoseKeys.HepatiteBGestante => updated with { RequiresAssessment = true, DoseLabel = "Completar 3 doses", Guidance = "Contar doses anteriores, inclusive penta. Completar 0–1–6 meses sem reiniciar. Até 15 anos: 0,5 mL; a partir de 16: 1,0 mL, IM. Manual, pp. 21–24, 95, 97, 100 e 103." },
                VaccineDoseKeys.DtInfantil or VaccineDoseKeys.DtAdolescente or VaccineDoseKeys.DtAdulto or VaccineDoseKeys.DtIdoso or VaccineDoseKeys.DtGestante => updated with { RequiresAssessment = true, RepeatIntervalMonths = d.RequiresPregnancy ? null : 120, IsRequiredForChildMetric = false, Guidance = "Conferir 3 doses com componente diftérico/tetânico. Completar dT: 60 dias (mínimo 30). Agenda oportuna: reforço aos 14 anos, 10 anos após DTP dos 4; depois decenal (5 anos em ferimento de risco). Gestante: incluir dTpa a partir da 20ª semana. Manual, pp. 72–74, 94–104." },
                _ => updated
            };
        }).ToList();

        void Add(string key, string name, string dose, VaccineSectionType section, int? min, int? max, string age, string description, string guidance,
            int? limit = null, bool assess = false, bool pregnant = false, bool live = false, bool seasonal = false, int? repeat = null, int? week = null, bool perPregnancy = false)
        {
            result.Add(new(key, name, dose, section, min, max,
                section == VaccineSectionType.Child && !assess && !seasonal && !repeat.HasValue,
                pregnant, age, description, guidance, true, assess, limit, live, seasonal, repeat, week, perPregnancy));
        }
        Add("VPC20-1", "Pneumo 20", "1ª dose", VaccineSectionType.Child, 2, 3, "2 meses", "Doenças pneumocócicas invasivas", "Transição: VPC 20 aos 2 meses, VPC 10 aos 4 e reforço VPC 20 aos 12. Resgate varia com idade/histórico. Esquema antigo completo com VPC 10 não exige VPC 20 adicional. IM, 0,5 mL. Manual, pp. 33–38.", 60, assess: true);
        Add("VPC20-R", "Pneumo 20", "Reforço ou resgate", VaccineSectionType.Child, 12, 14, "12 meses", "Doenças pneumocócicas invasivas", "Reforço: mínimo de 60 dias da dose anterior. Dos 13 meses aos 4 anos sem doses: dose única. Não repetir esquema VPC 10 já completo. Avaliar antes de indicar. Manual, pp. 34–36.", 60, assess: true);
        Add("ACWY-Infantil", "Meningo ACWY", "Reforço", VaccineSectionType.Child, 12, 14, "12 meses", AcwyDescription, "12 meses a 4 anos, 11 meses e 29 dias: uma dose, independentemente das doses antes de 1 ano. Men C já aplicada após 12 meses deve ser considerada na avaliação. IM, 0,5 mL. Manual, pp. 52–54 e 82–85.", 60, assess: true);
        Add("Covid3", "Covid-19 · Pfizer", "3ª dose", VaccineSectionType.Child, 9, 11, "9 meses", CovidDescription, "Mínimo de 8 semanas após D2. Conferir produto e doses anteriores. Rotina até 4 anos, 11 meses e 29 dias. IM, 0,3 mL, tampa amarela. Manual, pp. 45–48.", 60);
        Add("TripliceViral-Infantil2", "Tríplice viral", "2ª dose", VaccineSectionType.Child, 15, 23, "15 meses", TripliceViralDescription, "Rotina aos 15 meses junto com varicela em locais distintos. Tetraviral anterior deve ser considerada no histórico. SC, 0,5 mL, tríceps. Manual, pp. 55–58 e 84.", live: true);
        Add("Varicela-Infantil1", "Varicela", "1ª dose", VaccineSectionType.Child, 15, 23, "15 meses", VaricelaDescription, "Rotina aos 15 meses; segunda aos 4 anos. Tetraviral é alternativa na falta da monovalente ou alergia à proteína do leite; não duplicar dose já comprovada. SC, 0,5 mL, tríceps. Manual, pp. 58, 61–63 e 84.", 84, live: true);
        Add("VIP5", "VIP", "2º reforço", VaccineSectionType.Child, 48, 59, "4 anos", "Poliomielite", "Segundo reforço aos 4 anos, mínimo de 6 meses após o primeiro reforço. Rotina até 4 anos, 11 meses e 29 dias. IM, 0,5 mL. Manual, pp. 27–29 e 82–85.", 60);
        foreach (var section in new[] { VaccineSectionType.Child, VaccineSectionType.Teen, VaccineSectionType.Adult, VaccineSectionType.Elderly, VaccineSectionType.Pregnant })
        {
            var min = section switch { VaccineSectionType.Child => 6, VaccineSectionType.Teen => 120, VaccineSectionType.Adult => 240, VaccineSectionType.Elderly => 720, _ => (int?)null };
            var max = section switch { VaccineSectionType.Child => 71, VaccineSectionType.Teen => 239, VaccineSectionType.Adult => 719, _ => (int?)null };
            Add($"Influenza-{section}", "Influenza", "Dose da temporada", section, min, max, "Anual · conforme grupo", "Influenza e complicações", "Criança de 6 meses a 5 anos, 11 meses e 29 dias, gestante e 60+: rotina anual. Demais idades: grupos prioritários. Primeira vacinação infantil: 2 doses com 30 dias; depois uma por temporada. Conferir campanha e histórico. Manual, pp. 42–44.", section == VaccineSectionType.Child ? 72 : null, assess: true, pregnant: section == VaccineSectionType.Pregnant, seasonal: true);
            if (section != VaccineSectionType.Child)
                Add($"Covid-Periodica-{section}", "Covid-19", "Dose periódica", section, min, max, section == VaccineSectionType.Elderly ? "A cada 6 meses" : "Conforme grupo / gestação", CovidDescription, "60+ e imunocomprometidos: semestral. Gestante: uma por gestação, mínimo 6 meses da última. Comorbidades e demais grupos especiais: anual, mínimo 6 meses da última. Conferir produto e estratégia. Manual, pp. 45–48.", assess: true, pregnant: section == VaccineSectionType.Pregnant, repeat: section == VaccineSectionType.Elderly ? 6 : null, perPregnancy: section == VaccineSectionType.Pregnant);
        }
        Add("Dengue1", "Dengue · Takeda", "1ª dose", VaccineSectionType.Teen, 120, 179, "10 a 14 anos", "Dengue", "Duas doses com 3 meses de intervalo. Dengue prévia: esperar 6 meses para iniciar. Observar por 15–30 minutos após aplicação. Contraindicada na gestação e imunodepressão; avaliar lactação. SC, 0,5 mL. Manual, pp. 69–71 e 94.", 180, assess: true, live: true);
        Add("Dengue2", "Dengue · Takeda", "2ª dose", VaccineSectionType.Teen, 123, 179, "3 meses após D1", "Dengue", "Aprazar 3 meses após D1; não reiniciar se atrasou. Conferir idade e situações especiais para completar. Contraindicada na gestação. Manual, pp. 69–71.", 180, assess: true, live: true);
        Add("VPC20-Idoso", "Pneumo 20", "Dose única conforme histórico", VaccineSectionType.Elderly, 720, null, "60–84 anos: condições específicas; 85+: todos", "Doenças pneumocócicas invasivas", "60–84 anos: acamados/institucionalizados; 85+: todos não vacinados. Duas VPP 23 ou VPC 13/15 + VPP 23: completo. Uma VPP 23: VPC 20 após ≥1 ano; uma VPC 13/15: após ≥2 meses. Manual, pp. 33–36 e 100–101.", assess: true);
        Add("VPC20-Indigena", "Pneumo 20", "Dose única se indicada", VaccineSectionType.Child, 60, null, "Indígenas a partir de 5 anos", "Doenças pneumocócicas invasivas", "Indígenas a partir de 5 anos sem comprovação de conjugada. Conferir histórico e intervalos com outras pneumocócicas. Manual, pp. 33–36.", assess: true);
        Add("Covid-Periodica-Child", "Covid-19", "Dose periódica se indicada", VaccineSectionType.Child, 6, 119, "Após esquema básico · grupos especiais", CovidDescription, "Criança saudável com esquema básico completo não recebe doses periódicas. Imunocomprometida: semestral. Demais grupos especiais: anual, mínimo 6 meses da última dose. Avaliar produto conforme idade. Manual, pp. 45–48.", assess: true);
        Add("Influenza-Primaria2", "Influenza", "2ª dose da primeira vacinação", VaccineSectionType.Child, 7, 71, "30 dias após a primeira dose", "Influenza e complicações", "Somente na primeira vacinação infantil: segunda dose com 30 dias de intervalo. Se já recebeu dose em ano anterior, uma dose por temporada. Confirmar registros externos. Manual, pp. 42–44.", 72, assess: true, seasonal: true);
        foreach (var section in new[] { VaccineSectionType.Teen, VaccineSectionType.Adult, VaccineSectionType.Elderly })
            Add($"Varicela-Especial-{section}", "Varicela", "Esquema de grupo especial", section,
                section == VaccineSectionType.Teen ? 120 : section == VaccineSectionType.Adult ? 240 : 720,
                section == VaccineSectionType.Teen ? 239 : section == VaccineSectionType.Adult ? 719 : null,
                "Indígenas e trabalhadores da saúde", VaricelaDescription, "Fora da rotina infantil: indígenas e trabalhadores da saúde sem doença prévia. Duas doses; a partir de 13 anos, 8 semanas entre doses. Idoso: avaliar risco-benefício. Não aplicar na gestação. Manual, pp. 61–63, 95, 98 e 101.", assess: true, live: true);
        Add("VSR-Gestante", "VSR", "Uma dose em cada gestação", VaccineSectionType.Pregnant, null, null, "A partir da 28ª semana", "Formas graves de infecção por VSR no bebê", "Uma dose a partir da 28ª semana em cada gestação; sem limite final, exceto trabalho de parto ativo. IM, 0,5 mL, deltoide. Manual, pp. 78–79 e 103.", pregnant: true, week: 28, perPregnancy: true);
        return result.Select(d => d.DoseKey switch
        {
            VaccineDoseKeys.Penta2 => d with { PreviousDoseKey = VaccineDoseKeys.Penta1, MinimumIntervalDays = 30 },
            VaccineDoseKeys.Penta3 => d with { PreviousDoseKey = VaccineDoseKeys.Penta2, MinimumIntervalDays = 30 },
            VaccineDoseKeys.Vip2 => d with { PreviousDoseKey = VaccineDoseKeys.Vip1, MinimumIntervalDays = 30 },
            VaccineDoseKeys.Vip3 => d with { PreviousDoseKey = VaccineDoseKeys.Vip2, MinimumIntervalDays = 30 },
            VaccineDoseKeys.Vip4 => d with { PreviousDoseKey = VaccineDoseKeys.Vip3, MinimumIntervalMonths = 6 },
            "VIP5" => d with { PreviousDoseKey = VaccineDoseKeys.Vip4, MinimumIntervalMonths = 6 },
            VaccineDoseKeys.Dtp1 => d with { PreviousDoseKey = VaccineDoseKeys.Penta3, MinimumIntervalMonths = 6 },
            VaccineDoseKeys.Dtp2 => d with { PreviousDoseKey = VaccineDoseKeys.Dtp1, MinimumIntervalMonths = 6 },
            VaccineDoseKeys.Vrh2 => d with { PreviousDoseKey = VaccineDoseKeys.Vrh1, MinimumIntervalDays = 30 },
            VaccineDoseKeys.MeningoC2 => d with { PreviousDoseKey = VaccineDoseKeys.MeningoC1, MinimumIntervalDays = 30 },
            VaccineDoseKeys.Covid2 => d with { PreviousDoseKey = VaccineDoseKeys.Covid1, MinimumIntervalDays = 28 },
            "Covid3" => d with { PreviousDoseKey = VaccineDoseKeys.Covid2, MinimumIntervalDays = 56 },
            "TripliceViral-Infantil2" => d with { PreviousDoseKey = VaccineDoseKeys.TripliceViralInfantil, MinimumIntervalDays = 30 },
            _ => d
        }).ToArray();
    }

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
        if (ByKey.TryGetValue(doseKey, out var definition)) return definition;
        var parts = doseKey.Split('@');
        if (parts.Length != 2 || !ByKey.TryGetValue(parts[0], out definition)) return null;
        if (parts[1].StartsWith("application-", StringComparison.Ordinal) &&
            Guid.TryParseExact(parts[1][12..], "N", out _) && definition.AllowsPeriodicApplications)
            return definition with { DoseKey = doseKey, IsRequiredForChildMetric = false };
        var cycle = parts[1].Split('-');
        if (cycle.Length != 2 || !int.TryParse(cycle[1], out var number) || number < 1) return null;
        var valid = cycle[0] switch
        {
            "season" => definition.IsSeasonal && number is >= 1900 and <= 9999,
            "after" => definition.AllowsPeriodicApplications,
            "pregnancy" => definition.PerPregnancy,
            _ => false
        };
        return valid ? definition with { DoseKey = doseKey, IsRequiredForChildMetric = false } : null;
    }

    public static IEnumerable<VaccineDoseDefinition> GetApplicableDefinitions(int ageMonths, bool isPregnant)
    {
        return Definitions.Where(definition =>
            definition.IsRoutine &&
            (!definition.RequiresPregnancy || isPregnant) &&
            (!definition.MinAgeMonths.HasValue || ageMonths >= definition.MinAgeMonths.Value) &&
            (!definition.MaxAgeMonths.HasValue || ageMonths <= definition.MaxAgeMonths.Value));
    }

    public static IEnumerable<VaccineDoseDefinition> GetExpectedDefinitions(int ageMonths, bool isPregnant)
    {
        var definitions = ageMonths < 120
            ? Definitions.Where(definition => definition.Section == VaccineSectionType.Child && definition.IsRoutine && (definition.DoseKey != "VPC20-Indigena" || ageMonths >= 60))
            : GetApplicableDefinitions(ageMonths, isPregnant);

        if (isPregnant && ageMonths < 120)
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
