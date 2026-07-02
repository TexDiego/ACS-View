using ACS_View.Application.Security;
using ACS_View.Application.Querying;
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

Assert(!DashboardMetricCombinationRules.IsCombinableRootMetric(DashboardFilterKeys.Families), "Familias nao devem entrar em unioes.");
Assert(!DashboardMetricCombinationRules.IsCombinableRootMetric(DashboardFilterKeys.Residences), "Residencias nao devem entrar em unioes.");
Assert(!DashboardMetricCombinationRules.IsCombinableRootMetric(DashboardFilterKeys.EmptyResidences), "Residencias vazias nao devem entrar em unioes.");
Assert(!DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.Elderly, DashboardFilterKeys.ChildrenUnder6, targetIsHealth: false), "Idosos e criancas menores de 6 nao devem combinar.");
Assert(!DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.Women25To64, $"{DashboardFilterKeys.ConditionPrefix}Diabetes", targetIsHealth: true, sexModifier: "Masculino"), "Mulheres 25 a 64 nao deve aceitar modificador masculino.");
Assert(DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.Women25To64, $"{DashboardFilterKeys.ConditionPrefix}Diabetes", targetIsHealth: true), "Metrica geral compativel deve combinar com saude na aba de saude.");
Assert(!DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.BolsaFamilia, DashboardFilterKeys.NoHouse, targetIsHealth: true), "Uniao geral sem saude nao deve ser criada na aba de saude.");
Assert(DashboardMetricCombinationRules.CanCombine(DashboardFilterKeys.BolsaFamilia, DashboardFilterKeys.NoHouse, targetIsHealth: false), "Uniao geral compativel deve continuar disponivel na aba geral.");

Console.WriteLine("DashboardMetricCombinationRules tests passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
