using ACS_View.Application.Security;
using ACS_View.Application.Querying;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;

var secret = "SenhaForte123";
var otherSecret = "SenhaErrada123";

var firstHash = PasswordHasher.Hash(secret);
var secondHash = PasswordHasher.Hash(secret);

Assert(PasswordHasher.Verify(secret, firstHash.Hash, firstHash.Salt), "Senha correta deve validar.");
Assert(!PasswordHasher.Verify(otherSecret, firstHash.Hash, firstHash.Salt), "Senha incorreta não deve validar.");
Assert(firstHash.Hash != secondHash.Hash, "Hashes da mesma senha devem variar por salt.");
Assert(firstHash.Salt != secondHash.Salt, "Salts devem variar por hash.");
Assert(!PasswordHasher.Verify(secret, string.Empty, firstHash.Salt), "Hash vazio deve falhar.");
Assert(!PasswordHasher.Verify(secret, firstHash.Hash, string.Empty), "Salt vazio deve falhar.");

Console.WriteLine("PasswordHasher tests passed.");

var activeWhereParts = new List<string> { "p.UserId = ?" };
var activeParameters = new List<object> { 1 };
PatientFilterSqlBuilder.AddFilterClause(DashboardFilterKeys.All, activeWhereParts, activeParameters);
Assert(activeWhereParts.Contains("COALESCE(p.IsActive, 1) = 1"), "Filtro padrao deve listar pacientes ativos e legados sem status.");
Assert(!activeWhereParts.Contains("p.IsActive = 0"), "Filtro padrao nao deve incluir inativos.");

var inactiveWhereParts = new List<string> { "p.UserId = ?" };
var inactiveParameters = new List<object> { 1 };
PatientFilterSqlBuilder.AddFilterClause(DashboardFilterKeys.Inactive, inactiveWhereParts, inactiveParameters);
Assert(inactiveWhereParts.Contains("p.IsActive = 0"), "Filtro de inativos deve listar apenas pacientes inativos.");
Assert(!inactiveWhereParts.Contains("p.IsActive = 1"), "Filtro de inativos nao deve incluir ativos.");

var insulinWhereParts = new List<string> { "p.UserId = ?" };
var insulinParameters = new List<object> { 1 };
PatientFilterSqlBuilder.AddFilterClause($"{DashboardFilterKeys.ConditionPrefix}{HealthConditionCatalog.Insulinodependente}", insulinWhereParts, insulinParameters);
Assert(insulinWhereParts.Any(part => part.Contains("PatientInsulinDependency")), "Filtro de insulinodependente deve consultar a tabela relacional.");

var pregnantWhereParts = new List<string> { "p.UserId = ?" };
var pregnantParameters = new List<object> { 1 };
PatientFilterSqlBuilder.AddFilterClause(DashboardFilterKeys.Pregnant, pregnantWhereParts, pregnantParameters);
Assert(pregnantWhereParts.Any(part => part.Contains("PatientPregnancy")), "Filtro de gestantes deve consultar a tabela de gestacao.");
Assert(pregnantWhereParts.Any(part => part.Contains("pp.Status = 1")), "Filtro de gestantes deve considerar gestacao ativa.");
Assert(pregnantWhereParts.Any(part => part.Contains("NOT EXISTS") && part.Contains("puerperal")), "Filtro de gestantes deve excluir puerperio ativo.");
Assert(pregnantWhereParts.Any(part => part.Contains("puerperal.EndedAt") && part.Contains("62135596800")), "Filtro de gestantes deve normalizar data de parto ao excluir puerperas.");

var overdueVaccineWhereParts = new List<string> { "p.UserId = ?" };
var overdueVaccineParameters = new List<object> { 1 };
PatientFilterSqlBuilder.AddFilterClause(DashboardFilterKeys.ChildrenOverdueVaccines, overdueVaccineWhereParts, overdueVaccineParameters);
Assert(overdueVaccineWhereParts.Any(part => part.Contains("PatientVaccineDose")), "Filtro de vacinacao atrasada deve consultar registros de vacina.");
Assert(overdueVaccineWhereParts.Any(part => part.Contains("62135596800") && part.Contains("CAST(p.BirthDate AS INTEGER)")), "Filtro de vacinacao atrasada deve normalizar BirthDate salvo como ticks.");
Assert(overdueVaccineWhereParts.Any(part => part.Contains("substr(CAST(p.BirthDate AS TEXT), 7, 4)")), "Filtro de vacinacao atrasada deve aceitar datas textuais importadas.");
Assert(overdueVaccineWhereParts.Any(part => part.Contains("date('now', 'localtime')")), "Filtro de vacinacao atrasada deve calcular prazo vencido no SQLite.");
Assert(overdueVaccineParameters.Any(parameter => Equals(parameter, VaccineDoseKeys.BcgInfantil)), "Filtro de vacinacao atrasada deve enviar chaves de dose obrigatoria.");

var puerperalWhereParts = new List<string> { "p.UserId = ?" };
var puerperalParameters = new List<object> { 1 };
PatientFilterSqlBuilder.AddFilterClause(DashboardFilterKeys.ActivePuerperal, puerperalWhereParts, puerperalParameters);
Assert(puerperalWhereParts.Any(part => part.Contains("PatientPregnancy")), "Filtro de puerperio deve consultar a tabela de gestacao.");
Assert(puerperalWhereParts.Any(part => part.Contains("p.Sexo = 'Feminino'")), "Filtro de puerperio deve contabilizar mulheres.");
Assert(puerperalWhereParts.Any(part => part.Contains("'+42 days'")), "Filtro de puerperio deve respeitar janela de 42 dias.");
Assert(puerperalWhereParts.Any(part => part.Contains("pp.EndedAt")) &&
       puerperalWhereParts.Any(part => part.Contains("62135596800")), "Filtro de puerperio deve normalizar EndedAt salvo como ticks.");

Console.WriteLine("PatientFilterSqlBuilder tests passed.");

Assert(NisNumberRules.Normalize("123.45678.90-0") == "12345678900", "Normalizacao do NIS deve manter apenas digitos.");
Assert(NisNumberRules.Format("12345678900") == "123.45678.90-0", "Mascara do NIS deve usar o formato 000.00000.00-0.");
Assert(NisNumberRules.IsValid("12345678900"), "NIS com digito verificador correto deve validar.");
Assert(!NisNumberRules.IsValid("12345678901"), "NIS com digito verificador incorreto deve falhar.");
Assert(!NisNumberRules.IsValid("11111111111"), "NIS repetido nao deve validar.");

Console.WriteLine("NisNumberRules tests passed.");

var ruaParts = StreetAddressParser.SplitStreetType("Rua das Flores");
Assert(ruaParts.StreetType == "Rua", "Tipo de logradouro deve ser extraido de Rua.");
Assert(ruaParts.StreetName == "das Flores", "Nome da rua nao deve exibir o tipo de logradouro.");

var avenidaParts = StreetAddressParser.SplitStreetType("Avenida Brasil");
Assert(avenidaParts.StreetType == "Avenida", "Tipo de logradouro deve ser extraido de Avenida.");
Assert(avenidaParts.StreetName == "Brasil", "Nome da avenida nao deve exibir o tipo de logradouro.");

var travessaParts = StreetAddressParser.SplitStreetType("Tv. Santa Luzia");
Assert(travessaParts.StreetType == "Travessa", "Abreviacao de tipo de logradouro deve ser normalizada.");
Assert(travessaParts.StreetName == "Santa Luzia", "Nome de logradouro com abreviacao nao deve exibir o tipo.");

var semTipoParts = StreetAddressParser.SplitStreetType("Quinze de Novembro");
Assert(semTipoParts.StreetType == string.Empty, "Logradouro sem tipo deve manter tipo vazio.");
Assert(semTipoParts.StreetName == "Quinze de Novembro", "Logradouro sem tipo deve manter o nome original.");

Console.WriteLine("StreetAddressParser tests passed.");

Assert(CepNumberRules.Normalize("12345-678") == "12345678", "Normalizacao do CEP deve manter apenas digitos.");
Assert(CepNumberRules.IsValid("12345-678"), "CEP formatado deve validar.");
Assert(!CepNumberRules.IsValid("1234567"), "CEP incompleto nao deve validar.");

Console.WriteLine("CepNumberRules tests passed.");

var vaccineDefinition = new VaccineDoseDefinition(
    "TEST",
    "Teste",
    "1 dose",
    VaccineSectionType.Child,
    2,
    3,
    true);
var birthDate = new DateTime(2026, 1, 1);
Assert(
    VaccineStatusCalculator.Calculate(birthDate, new DateTime(2026, 2, 20), vaccineDefinition, null) == VaccineStatus.NotYetDue,
    "Dose antes da idade recomendada deve ficar como ainda nao indicada.");
Assert(
    VaccineStatusCalculator.Calculate(birthDate, new DateTime(2026, 3, 10), vaccineDefinition, null) == VaccineStatus.Due,
    "Dose dentro do prazo sem aplicacao deve ficar pendente.");
Assert(
    VaccineStatusCalculator.Calculate(birthDate, new DateTime(2026, 4, 2), vaccineDefinition, null) == VaccineStatus.Late,
    "Dose apos o prazo maximo sem aplicacao deve ficar atrasada.");
Assert(
    VaccineStatusCalculator.Calculate(birthDate, new DateTime(2026, 4, 2), vaccineDefinition, new DateTime(2026, 3, 15)) == VaccineStatus.Applied,
    "Dose aplicada dentro do prazo deve ficar aplicada.");
Assert(
    VaccineStatusCalculator.Calculate(birthDate, new DateTime(2026, 5, 1), vaccineDefinition, new DateTime(2026, 4, 5)) == VaccineStatus.AppliedLate,
    "Dose aplicada apos o prazo maximo deve ficar aplicada com atraso.");

Console.WriteLine("VaccineStatusCalculator tests passed.");

Assert(!DashboardMetricCombinationRules.IsCombinableRootMetric(DashboardFilterKeys.Families), "Familias nao devem entrar em unioes.");
Assert(!DashboardMetricCombinationRules.IsCombinableRootMetric(DashboardFilterKeys.Residences), "Residencias nao devem entrar em unioes.");
Assert(!DashboardMetricCombinationRules.IsCombinableRootMetric(DashboardFilterKeys.EmptyResidences), "Residencias vazias nao devem entrar em unioes.");
Assert(!DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.Elderly, DashboardFilterKeys.ChildrenUnder6, targetIsHealth: false), "Idosos e criancas menores de 6 nao devem combinar.");
Assert(!DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.Women25To64, $"{DashboardFilterKeys.ConditionPrefix}Diabetes", targetIsHealth: true, sexModifier: "Masculino"), "Mulheres 25 a 64 nao deve aceitar modificador masculino.");
Assert(DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.Women25To64, $"{DashboardFilterKeys.ConditionPrefix}Diabetes", targetIsHealth: true), "Metrica geral compativel deve combinar com saude na aba de saude.");
Assert(!DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.BolsaFamilia, DashboardFilterKeys.NoHouse, targetIsHealth: true), "Uniao geral sem saude nao deve ser criada na aba de saude.");
Assert(DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.BolsaFamilia, DashboardFilterKeys.NoHouse, targetIsHealth: false), "Uniao geral compativel deve continuar disponivel na aba geral.");
Assert(DashboardMetricCombinationRules.IsHealthMetric(DashboardFilterKeys.ChildrenOverdueVaccines), "Vacinacao atrasada deve ser tratada como metrica de saude.");
Assert(!DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.ChildrenOverdueVaccines, DashboardFilterKeys.Elderly, targetIsHealth: true), "Vacinacao infantil atrasada nao deve combinar com idosos.");
Assert(DashboardMetricCombinationRules.CanCombine([DashboardFilterKeys.NoHouse], targetIsHealth: false, minimumAgeModifier: 1), "Um indicador com modificador deve poder criar metrica derivada.");
Assert(!DashboardMetricCombinationRules.CanCombine([DashboardFilterKeys.NoHouse], targetIsHealth: false), "Um indicador sem modificador nao deve criar metrica duplicada.");
Assert(DashboardMetricCombinationRules.CanCombine([DashboardFilterKeys.NoHouse, DashboardFilterKeys.NoFamily, DashboardFilterKeys.BolsaFamilia], targetIsHealth: false), "Tres indicadores gerais compativeis devem poder ser unidos.");
Assert(!DashboardMetricCombinationRules.CanCombine([DashboardFilterKeys.NoHouse, DashboardFilterKeys.NoFamily, DashboardFilterKeys.BolsaFamilia, DashboardFilterKeys.ChildrenUnder6], targetIsHealth: false), "Mais de tres indicadores nao devem ser aceitos.");

Console.WriteLine("DashboardMetricCombinationRules tests passed.");

var strongVisitCareLines = new[]
{
    VisitCareLineType.Hypertension,
    VisitCareLineType.Diabetes,
    VisitCareLineType.Elderly,
    VisitCareLineType.BolsaFamilia
};
Assert(strongVisitCareLines.All(careLine => VisitScoringRuleCatalog.GetRule(careLine) is not null), "Paciente com hipertensao, diabetes, idoso e Bolsa Familia deve ter regras de sugestao para todas as linhas de cuidado.");
Assert(VisitScoringRuleCatalog.GetRule(VisitCareLineType.BolsaFamilia)?.RequiredVisits == 2, "Bolsa Familia deve gerar sugestao propria de visita.");
Assert(VisitScoringRuleCatalog.GetRule(VisitCareLineType.Child)?.Description.Contains("ate 2 anos", StringComparison.OrdinalIgnoreCase) == true ||
       VisitScoringRuleCatalog.GetRule(VisitCareLineType.Child)?.Description.Contains("até 2 anos", StringComparison.OrdinalIgnoreCase) == true, "Sugestao infantil deve seguir a matriz de criancas ate 2 anos.");
Assert(VisitScoringRuleCatalog.GetRule(VisitCareLineType.Child)?.DeadlineRule == "ChildTwoStep", "Sugestao infantil deve manter a regra infantil da matriz, nao a metrica de menores de 6 anos.");
Assert(VisitScoringRuleCatalog.GetRule(VisitCareLineType.NoVulnerability)?.RequiredVisits == 1, "Pacientes sem criterios de vulnerabilidade devem exigir 1 visita no mes.");
Assert(VisitScoringRuleCatalog.GetRule(VisitCareLineType.NoVulnerability)?.Points == 10, "Pacientes sem criterios de vulnerabilidade devem ter pontuacao generica menor.");
Assert(VisitPriorityCalculator.Calculate([VisitCareLineType.NoVulnerability]).Factor == 1.0m, "Pacientes sem criterios devem manter prioridade base 1.0.");
Assert(VisitPriorityCalculator.Calculate(strongVisitCareLines).Factor == 2.5m, "Idoso beneficiario deve receber fator de prioridade 2.5.");
Assert(strongVisitCareLines.Sum(careLine => VisitScoringRuleCatalog.GetRule(careLine)?.Points ?? 0) >= 80, "Paciente com multiplos fatores fortes deve acumular pontuacao alta.");

Console.WriteLine("VisitScoringRuleCatalog tests passed.");

var pregnancy = new ACS_View.Domain.Entities.PatientPregnancy
{
    PatientId = 10,
    LastMenstrualPeriod = new DateTime(2026, 1, 1),
    ExpectedBirthDate = null,
    Status = PregnancyStatus.Active,
    ManualRisk = PregnancyRisk.NotInformed
};
Assert(PregnancyCalculator.CalculateExpectedBirthDate(new DateTime(2026, 1, 1)) == new DateTime(2026, 10, 8), "DPP deve ser DUM mais 280 dias.");
Assert(PregnancyCalculator.CalculateGestationalAge(pregnancy, new DateTime(2026, 2, 20)) == new GestationalAge(7, 1), "Idade gestacional deve calcular semanas e dias.");
Assert(new GestationalAge(35, 4).ToString() == "35 semanas e 4 dias", "Idade gestacional deve exibir semanas e dias por extenso.");
Assert(new GestationalAge(32, 0).ToString() == "32 semanas", "Idade gestacional sem dias restantes deve exibir apenas semanas.");
Assert(PregnancyCalculator.CalculateTrimester(pregnancy, new DateTime(2026, 2, 20)) == 1, "Gestacao com 7 semanas deve estar no primeiro trimestre.");
Assert(PregnancyCalculator.CalculateTrimester(pregnancy, new DateTime(2026, 5, 1)) == 2, "Gestacao com 17 semanas deve estar no segundo trimestre.");
Assert(PregnancyCalculator.CalculateTrimester(pregnancy, new DateTime(2026, 8, 1)) == 3, "Gestacao com 30 semanas deve estar no terceiro trimestre.");

var endedPregnancy = new ACS_View.Domain.Entities.PatientPregnancy
{
    PatientId = pregnancy.PatientId,
    LastMenstrualPeriod = pregnancy.LastMenstrualPeriod,
    ExpectedBirthDate = pregnancy.ExpectedBirthDate,
    Status = PregnancyStatus.Ended,
    EndType = PregnancyEndType.Birth,
    EndedAt = new DateTime(2026, 7, 1)
};
Assert(PregnancyCalculator.IsPuerperal(endedPregnancy, new DateTime(2026, 7, 20)), "Parto dentro de 42 dias deve ser puerperio ativo.");
Assert(!PregnancyCalculator.IsPuerperal(endedPregnancy, new DateTime(2026, 8, 20)), "Parto apos 42 dias nao deve ser puerperio ativo.");
Assert(PregnancyCalculator.CalculatePostpartumDays(endedPregnancy, new DateTime(2026, 7, 20)) == 19, "Dias pos-parto devem ser calculados.");
Assert(PregnancyCalculator.CalculatePuerperiumEndDate(endedPregnancy) == new DateTime(2026, 8, 12), "Fim do puerperio deve somar 42 dias.");

var mother = new ACS_View.Domain.Entities.Patient { Id = 7, BirthDate = new DateTime(1990, 1, 1) };
var children = new[]
{
    new ACS_View.Domain.Entities.Patient { Id = 8, MotherPatientId = 7 },
    new ACS_View.Domain.Entities.Patient { Id = 9, MotherPatientId = 7 },
    new ACS_View.Domain.Entities.Patient { Id = 10, MotherPatientId = 99 }
};
Assert(PregnancyCalculator.CalculateRegisteredChildrenCount(7, children) == 2, "Filhos cadastrados devem ser contados por MotherPatientId.");

var highRiskSuggestion = PregnancyRiskSuggestionCalculator.Calculate(
    mother,
    new ACS_View.Domain.Entities.PatientPregnancy
    {
        LastMenstrualPeriod = pregnancy.LastMenstrualPeriod,
        ExpectedBirthDate = pregnancy.ExpectedBirthDate,
        Status = pregnancy.Status,
        InformedChildrenCount = 4
    },
    [HealthConditionCatalog.Hipertensao, HealthConditionCatalog.Diabetes],
    registeredChildrenCount: 2,
    referenceDate: new DateTime(2026, 1, 1));
Assert(highRiskSuggestion.Risk == PregnancyRisk.HighRisk, "Sugestao de risco deve chegar a alto risco por pontuacao.");
Assert(highRiskSuggestion.Reasons.Count >= 3, "Sugestao de risco deve explicar motivos.");

var missingDateSuggestion = PregnancyRiskSuggestionCalculator.Calculate(
    mother,
    new ACS_View.Domain.Entities.PatientPregnancy(),
    [],
    registeredChildrenCount: 0,
    referenceDate: new DateTime(2026, 1, 1));
var informedDateSuggestion = PregnancyRiskSuggestionCalculator.Calculate(
    mother,
    new ACS_View.Domain.Entities.PatientPregnancy
    {
        LastMenstrualPeriod = new DateTime(2026, 1, 1),
        ExpectedBirthDate = PregnancyCalculator.CalculateExpectedBirthDate(new DateTime(2026, 1, 1))
    },
    [],
    registeredChildrenCount: 0,
    referenceDate: new DateTime(2026, 1, 1));
Assert(missingDateSuggestion.Reasons.Any(reason => reason.Contains("DUM", StringComparison.OrdinalIgnoreCase)), "DUM/DPP ausentes devem gerar motivo de atencao.");
Assert(!informedDateSuggestion.Reasons.Any(reason => reason.Contains("DUM", StringComparison.OrdinalIgnoreCase)), "DUM/DPP informadas nao devem manter motivo de ausencia.");

Console.WriteLine("PregnancyCalculator tests passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
