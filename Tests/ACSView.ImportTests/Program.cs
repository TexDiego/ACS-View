using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.ValueObjects;
using ACS_View.UseCases.Services;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

var patients = new List<Patient>();
var houses = new List<House>();
var conditions = new List<PatientConditions>();
var history = new List<ImportHistory>();
static T Clone<T>(T item) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(item))!;
var patientService = Proxy.For<IPatientService>((m, a) => m switch
{
    "GetAllPatients" => Task.FromResult<List<Patient>?>(patients.Select(Clone).ToList()),
    "CreatePatient" => SavePatient((Patient)a[0]!, true),
    "UpdatePatient" => SavePatient((Patient)a[0]!, false),
    _ => throw new Exception(m)
});
Task SavePatient(Patient p, bool create)
{
    if (create) p.Id = patients.Count + 1;
    else patients.RemoveAll(x => x.Id == p.Id);
    patients.Add(Clone(p));
    return Task.CompletedTask;
}
var houseService = Proxy.For<IHouseService>((m, a) => m switch
{
    "GetAllHousesAsync" => Task.FromResult(houses.Select(Clone).ToList()),
    "SaveHouseAsync" => SaveHouse((House)a[0]!),
    "UpdateHouseAsync" => UpdateHouse((House)a[0]!),
    _ => throw new Exception(m)
});
Task SaveHouse(House h) { h.CasaId = houses.Count + 1; houses.Add(Clone(h)); return Task.CompletedTask; }
Task UpdateHouse(House h) { houses.RemoveAll(x => x.CasaId == h.CasaId); houses.Add(Clone(h)); return Task.CompletedTask; }
var conditionRepo = Proxy.For<ISQLiteConditionsRepository>((m, a) => m switch
{
    "GetConditionsByPatientIdAsync" => Task.FromResult(conditions.Where(c => c.PatientId == (int)a[0]!).Select(Clone).ToList()),
    "InsertConditionAsync" => SaveCondition((PatientConditions)a[0]!),
    "DeleteConditionAsync" => DeleteCondition((int)a[0]!),
    _ => throw new Exception(m)
});
Task SaveCondition(PatientConditions c) { c.Id = conditions.Select(x => x.Id).DefaultIfEmpty(0).Max()+1; conditions.Add(Clone(c)); return Task.CompletedTask; }
Task DeleteCondition(int id) { conditions.RemoveAll(c => c.Id == id); return Task.CompletedTask; }
var cep = Proxy.For<ICepService>((m, a) => Task.FromResult<House?>(new House { Rua = "Rua Brasil", Bairro = "Centro", Cidade = "São Paulo", Estado = "SP" }));
var family = Proxy.For<IFamilyService>((m, a) => m == "GetMaxIdAsync" ? Task.FromResult(patients.Where(p => p.HouseId == (int)a[0]!).Select(p => p.FamilyId).DefaultIfEmpty(0).Max()) : throw new Exception(m));
var benefits = Proxy.For<IPatientBolsaFamiliaRepository>((m, a) => m == "GetByPatientIdAsync" ? Task.FromResult<PatientBolsaFamilia?>(null) : Task.CompletedTask);
var journal = Proxy.For<IImportHistoryService>((m, a) => { history.Add(Clone((ImportHistory)a[0]!)); return Task.CompletedTask; });
var service = new PatientImportService(patientService, cep, conditionRepo, benefits, new SpreadsheetReader(), new PatientFamilyLinkResolver(patientService, houseService, family), journal);

async Task<PatientImportResultDto> Import(string[][] rows, CancellationToken cancellation = default)
{
    using var stream = Workbook(rows);
    return await service.ImportAsync(stream, new PatientImportColumnMapDto { SourceFileName = "teste.xlsx" }, cancellationToken: cancellation);
}
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS: " + message); }

var first = await Import([
    ["CADASTRO NOME", "NOME DA MÃE", "CADASTRO DN", "SEXO", "DIABETES", "OUTRA CONDICAO DE SAUDE", "CEP", "NUMERO IMOVEL", "RESP. FAMILIAR CNS"],
    ["José Silva", "Maria Silva", "01/02/2010", "M", "sim", "Observação A", "01001000", "10", "222"],
    ["José Silva", "Maria Silva", "01/02/2010", "", "", "Observação B", "", "", ""]
]);
Check(first.Entries.Any(e => e.EntityType == "Residência" && e.Status == "Importado"), "Residência criada na importação de pacientes também aparece na auditoria");
Check(patients.Count == 1 && first.MergedCount == 1, "Linhas repetidas complementam o mesmo cadastro");
Check(patients[0].Observacao.Contains("A") && patients[0].Observacao.Contains("B"), "Observações complementares preservadas");
Check(conditions.Count == 1 && patients[0].Sexo == "Masculino", "Campos vazios preservam condições e sexo");
Check(houses.Count == 1 && patients[0].HouseId == 1, "Residência criada por CEP e número");

var second = await Import([
    ["USUARIO SAUDE NOME", "CNS", "NOME DA MAE", "DATA NASCIMENTO", "CADASTRO SEXO", "USA ALCOOL", "DIABETES"],
    ["Jose Silva", "111", "Maria Silva", "01/02/2010", "masculino", "SIM", "NÃO"]
]);
Check(patients.Count == 1 && patients[0].SusNumber == "111" && second.MergedCount == 1, "CNS posterior encontra identidade sem duplicar");
Check(conditions.Count == 1 && conditions[0].Description == HealthConditionCatalog.Alcoolatra, "Sim e não explícitos atualizam só as condições presentes");
var mother = await Import([
    ["cadastro nome", "cadastro cns", "cadastro dn", "sexo", "cep", "numero imovel", "resp. familiar?"],
    ["Maria Silva", "222", "10/03/1980", "F", "01001000", "10", "SIM"]
]);
var child = patients.Single(p => p.HasSusNumber("111"));
var parent = patients.Single(p => p.SusNumber == "222");
Check(child.MotherPatientId == parent.Id, "Mãe importada depois resolve vínculo pendente");
Check(child.FamilyResponsiblePatientId == parent.Id && child.FamilyId == parent.FamilyId && child.FamilyId > 0, "Responsável posterior calcula família sem duplicar residência");

var repeat = await Import([["nome", "cns", "usa alcool"], ["Jose Silva", "111", "SIM"]]);
Check(repeat.MergedCount == 0 && repeat.IgnoredCount == 1 && repeat.Entries.Single().Reason.Contains("nenhuma atualização"), "Paciente repetido sem alterações é ignorado e nunca mesclado");
Check(repeat.ImportedCount == 0 && conditions.Count == 1 && houses.Count == 1, "Reimportação não duplica cadastros, condições ou residências");
var conflict = await Import([
    ["nome", "cns", "nome da mae", "data nascimento", "diabetes", "coluna desconhecida"],
    ["Jose Silva", "999", "Maria Silva", "01/02/2010", "talvez", "valor externo"]
]);
Check(patients.Count == 2 && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999"]) && !conflict.Errors.Any(e => e.Contains("CNS divergente")), "Identidade igual acumula todos os CNS sem duplicar paciente");
Check(conflict.Errors.Any(e => e.Contains("talvez")) && !conflict.Errors.Concat(conflict.Details).Any(e => e.Contains("valor externo") || e.Contains("coluna desconhecida")), "Valores inválidos são registrados e colunas sem mapeamento não geram ruído");

var aliases = await Import([["cadastro nome", "usuario saude nome", "cns"], ["", "Carlos Souza", "333"]]);
Check(patients.Any(p => p.SusNumber == "333" && p.Name == "Carlos Souza"), "Alias vazio usa a outra coluna equivalente");
Check(!ImportColumnAliases.Matches("resp. familiar cns", "cns"), "CNS do responsável não é confundido com CNS do paciente");

patients.Add(new Patient { Id = 90, Name = "Duplicado", MotherName = "Outra Mãe", BirthDate = new DateTime(1980, 1, 1), SusNumber = "888" });
patients.Add(new Patient { Id = 91, Name = "Duplicado", MotherName = "Outra Mãe", BirthDate = new DateTime(1980, 1, 1), SusNumber = "777" });
var ambiguous = await Import([["nome", "nome da mae", "data nascimento"], ["Duplicado", "Outra Mãe", "01/01/1980"]]);
Check(ambiguous.IgnoredCount == 1 && ambiguous.Errors.Any(e => e.Contains("vários cadastros")), "Duplicatas preexistentes ambíguas exigem revisão");

using var cts = new CancellationTokenSource(); cts.Cancel();
try { await Import([["nome"], ["Teste"]], cts.Token); throw new Exception("Cancelamento esperado"); }
catch (OperationCanceledException) { }
Check(history.Last().Status.StartsWith("Cancelada") && !string.IsNullOrWhiteSpace(history.Last().FinishedAt), "Cancelamento mantém histórico e carimbo de horário");
Check(history.All(h => h.FileName == "teste.xlsx") && history.Any(h => JsonSerializer.Deserialize<PatientImportResultDto>(h.ReportJson)!.MergedCount > 0), "Histórico contém arquivo e detalhes de mesclagens");
Console.WriteLine("Import integration tests passed.");

var houseImporter = new HouseImportService(houseService, cep, new SpreadsheetReader(), journal);
using (var workbook = Workbook([["cep", "numero imovel", "complemento"], ["01001000", "10", ""]]))
{
    var report = await houseImporter.ImportAsync(workbook, new HouseImportColumnMapDto { SourceFileName = "teste.xlsx" });
    Check(report.MergedCount == 0 && report.IgnoredCount == 1 && report.Entries.Single().Status == "Ignorado" && houses.Count == 1, "Residência já cadastrada sem alterações é ignorada com motivo no histórico");
}
var fatherImport = await Import([["nome", "cns", "data nascimento"], ["Pedro Silva", "444", "01/01/1980"]]);
var fatherLink = await Import([["nome", "cns", "nome do pai"], ["Jose Silva", "111", "Pedro Silva"]]);
Check(patients.Single(p => p.HasSusNumber("111")).FatherPatientId == patients.Single(p => p.SusNumber == "444").Id, "Pai associado quando o nome está disponível");
var allConditions = await Import([
    ["nome", "cns", "esta gestante", "hipertensao", "tuberculose", "hanseniase", "esta acamado", "esta domiciliado", "diagnostico problema de saude", "fumante", "cancer", "usa drogas"],
    ["Maria Silva", "222", "sim", "sim", "sim", "sim", "sim", "sim", "sim", "sim", "sim", "sim"]
]);
Check(conditions.Count(c => c.PatientId == parent.Id) == 10, "Todos os aliases de condições informados são reconhecidos");
var badDate = await Import([["nome", "cns", "data nascimento"], ["Jose Silva", "111", "31/99/2020"]]);
Check(badDate.Errors.Any(e => e.Contains("data de nascimento inválida")) && patients.Single(p => p.HasSusNumber("111")).BirthDate == new DateTime(2010, 2, 1), "Data inválida é registrada e preserva nascimento anterior");
Console.WriteLine("All import regression checks passed.");

patients.Single(p => p.HasSusNumber("111")).StatusReason = "Informação manual";
patients.Single(p => p.HasSusNumber("111")).IsActive = false;
await Import([["nome", "cns"], ["Jose Silva", "111"]]);
Check(patients.Single(p => p.HasSusNumber("111")).StatusReason == "Informação manual" && !patients.Single(p => p.HasSusNumber("111")).IsActive, "Campos de edição manual ausentes na planilha são preservados");
var spacedRows = await Import([[], [], [], [], ["nome", "cns", "data nascimento"], [], [], [], [], ["Jose Silva", "111", "inválida"]]);
Check(spacedRows.Errors.Any(e => e.StartsWith("Linha 10: data de nascimento inválida")), "Relatório respeita a linha física do Excel com linhas vazias");
var originalCount = patients.Count;
var alternateSus = await Import([["nome", "cns"], ["Jose Silva", "999"]]);
Check(patients.Count == originalCount && alternateSus.MergedCount == 0 && alternateSus.IgnoredCount == 1 && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999"]), "CNS secundário localiza cadastro sem dados de identidade e preserva os demais números");
var multipleInCell = await Import([["nome", "cns"], ["Jose Silva", "111; 999, 555 / 5.55"]]);
Check(patients.Count == originalCount && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999", "555"]), "Vários CNS na célula são normalizados e deduplicados");
var sameSheet = await Import([["nome", "cns"], ["Jose Silva", "555; 666"], ["Jose Silva", "666"]]);
Check(sameSheet.MergedCount == 1 && sameSheet.IgnoredCount == 1 && patients.Count == originalCount && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999", "555", "666"]), "Novo CNS fica disponível para linhas seguintes da mesma importação");
var mixedPatients = await Import([["nome", "cns"], ["Jose Silva", "111; 222"]]);
Check(mixedPatients.IgnoredCount == 1 && patients.Single(p => p.Id == child.Id).SusNumbers.SequenceEqual(["111", "999", "555", "666"]), "CNS de pessoas diferentes exige revisão e não mistura cadastros");

await Import([["nome", "cns", "resp. familiar cns", "cep", "numero imovel"], ["Dependente Novo", "700", "223", "01001000", "10"]]);
var dependent = patients.Single(p => p.HasSusNumber("700"));
Check(dependent.FamilyResponsiblePatientId is null, "Vínculo fica pendente enquanto CNS alternativo do responsável é desconhecido");
var responsibleAlias = await Import([["nome", "cns"], ["Maria Silva", "222; 223; 2.23"]]);
var updatedResponsible = patients.Single(p => p.Id == parent.Id);
dependent = patients.Single(p => p.Id == dependent.Id);
Check(updatedResponsible.SusNumbers.SequenceEqual(["222", "223"]) && dependent.FamilyResponsiblePatientId == parent.Id && dependent.FamilyId == updatedResponsible.FamilyId, "Responsável recebe CNS alternativo e resolve dependente importado antes");
var responsibleRepeat = await Import([["nome", "cns", "resp. familiar cns"], ["Dependente Novo", "700", "223"]]);
Check(!responsibleRepeat.Errors.Any(e => e.Contains("diverge") || e.Contains("não associado")) && patients.Single(p => p.Id == dependent.Id).FamilyResponsiblePatientId == parent.Id, "Reimportação pelo CNS secundário reconhece vínculo existente");
var lateDependent = await Import([["nome", "cns", "resp. familiar cns"], ["Dependente Posterior", "701", "223"]]);
Check(patients.Single(p => p.HasSusNumber("701")).FamilyResponsiblePatientId == parent.Id, "Novo dependente encontra responsável já cadastrado pelo CNS secundário");

var legacyPatient = new Patient { SusNumber = "123.4567.8901.2345" };
Check(legacyPatient.SusNumbers.SequenceEqual(["123456789012345"]), "Cadastro legado com único CNS continua compatível");
legacyPatient.SusNumbers = ["123456789012345", "987.6543.2109.8765", "123456789012345"];
var roundtripPatient = Clone(legacyPatient);
Check(roundtripPatient.SusNumbers.SequenceEqual(["123456789012345", "987654321098765"]) && roundtripPatient.PrimarySusNumber == "123456789012345", "Coleção deduplicada mantém dados no ciclo de serialização");
SQLitePCL.Batteries_V2.Init();
using (var database = new SQLite.SQLiteConnection(":memory:"))
{
    database.Execute("CREATE TABLE Patient (Id INTEGER PRIMARY KEY AUTOINCREMENT, SusNumber TEXT)");
    database.Execute("INSERT INTO Patient (SusNumber) VALUES (?)", "123.4567.8901.2345");
    database.CreateTable<Patient>();
    var persisted = database.Table<Patient>().Single();
    Check(persisted.SusNumbers.SequenceEqual(["123456789012345"]), "SQLite existente carrega CNS legado sem perda após atualização do modelo");
    persisted.AddSusNumbers("987654321098765; 987.6543.2109.8765");
    database.Update(persisted);
    var reloaded = database.Table<Patient>().Single();
    Check(reloaded.SusNumbers.SequenceEqual(["123456789012345", "987654321098765"]), "SQLite persiste e recarrega coleção sem duplicação");
    Check(database.ExecuteScalar<int>("SELECT COUNT(*) FROM Patient WHERE SusNumber LIKE ?", "%987654321098765%") == 1, "Busca SQLite encontra CNS secundário na coluna compatível");
}
Console.WriteLine("All import, persistence and multiple-CNS checks passed.");

var linkPatientId = patients.Max(p => p.Id) + 1;
patients.Add(new Patient { Id = linkPatientId, Name = "Só Vínculo", SusNumber = "950" });
var linkOnly = await Import([["nome", "cns", "cep", "numero imovel"], ["Só Vínculo", "950", "01001000", "10"]]);
Check(linkOnly.MergedCount == 1 && linkOnly.IgnoredCount == 0 && linkOnly.Entries.Single(e => e.EntityType == "Paciente").Changes.Contains("Residência"), "Alteração apenas no vínculo de residência é mesclagem auditada depois da resolução");
var failingConditions = Proxy.For<ISQLiteConditionsRepository>((_, _) => throw new InvalidOperationException("Falha simulada após salvar paciente"));
var partialImporter = new PatientImportService(patientService, cep, failingConditions, benefits, new SpreadsheetReader(), new PatientFamilyLinkResolver(patientService, houseService, family), journal);
using (var book = Workbook([["nome", "cns", "diabetes"], ["Parcial", "951", "sim"]]))
{
    var report = await partialImporter.ImportAsync(book, new());
    Check(report.ImportedCount == 1 && report.IgnoredCount == 0 && report.Entries.Single().Reason.Contains("parcial") && patients.Any(p => p.HasSusNumber("951")), "Falha após criar paciente mantém auditoria da gravação parcial e não declara ignorado");
}
using (var book = Workbook([["nome", "cns", "diabetes"], ["Parcial Atualizado", "951", "sim"]]))
{
    var report = await partialImporter.ImportAsync(book, new());
    Check(report.MergedCount == 1 && report.IgnoredCount == 0 && report.Entries.Single().Changes.Contains("Parcial Atualizado"), "Falha após atualizar cadastro preserva o antes/depois e registra mesclagem parcial");
}

// Audit outcomes and CEP precedence use real import services and a persistent SQLite cache.
var lookupCalls = 0;
var remoteCep = Proxy.For<ICepService>((_, _) =>
{
    lookupCalls++;
    return Task.FromResult<House?>(new House { Rua = "Oficial", Bairro = "Centro", Cidade = "São Paulo", Estado = "SP" });
});
var priorityImporter = new HouseImportService(houseService, remoteCep, new SpreadsheetReader(), journal);
using (var book = Workbook([
    ["cep", "rua", "numero imovel", "bairro", "cidade", "estado", "pais"],
    ["12345-678", "Planilha", "80", "Outro", "Outra Cidade", "RJ", "Brasil"],
    ["12345678", "Planilha", "81", "Outro", "Outra Cidade", "RJ", "Brasil"]]))
{
    var report = await priorityImporter.ImportAsync(book, new());
    Check(lookupCalls == 1 && report.ImportedCount == 2 && houses.Where(h => h.CEP == "12345678").All(h => h.Rua == "Oficial" && h.Cidade == "São Paulo"), "CEP tem precedência mesmo com planilha completa; CEP formatado é consultado uma vez por lote");
    Check(report.Entries.All(e => e.Status == "Importado" && e.RecordId > 0), "Auditoria identifica linha e cadastro das residências criadas");
}
var offlineCalls = 0;
var offlineCep = Proxy.For<ICepService>((_, _) => { offlineCalls++; return Task.FromException<House?>(new HttpRequestException("offline")); });
var offlineImporter = new HouseImportService(houseService, offlineCep, new SpreadsheetReader(), journal);
using (var book = Workbook([
    ["cep", "rua", "numero imovel", "bairro", "cidade", "estado"],
    ["87654-321", "Rua Manual", "20", "Bairro Manual", "Cidade Manual", "SP"],
    ["87654321", "Rua Manual", "21", "Bairro Manual", "Cidade Manual", "SP"],
    ["87654321", "", "22", "", "", ""]]))
{
    var report = await offlineImporter.ImportAsync(book, new());
    Check(offlineCalls == 1 && report.ImportedCount == 2 && report.IgnoredCount == 1, "Falha de CEP é lembrada no lote; endereço completo da planilha permite importar");
    Check(report.Entries.Single(e => e.RowNumber == 4).Status == "Ignorado" && report.Entries.Single(e => e.RowNumber == 4).Reason.Contains("falta de informações"), "Residência incompleta é ignorada com motivo claro na linha física");
}
houses.Single(h => h.CEP == "12345678" && h.NumeroCasa == "80").Bairro = "Antigo";
using (var book = Workbook([["cep", "numero imovel"], ["12345678", "80"]]))
{
    var report = await priorityImporter.ImportAsync(book, new());
    Check(report.MergedCount == 1 && report.UpdatedCount == 1 && report.IgnoredCount == 0 && report.Entries.Single().Changes.Contains("Antigo"), "Mesclagem de residência registra valor anterior e novo apenas quando houve alteração");
}
using (var book = Workbook([["cep", "numero imovel"], ["12345678", "80"]]))
{
    var report = await priorityImporter.ImportAsync(book, new());
    Check(report.MergedCount == 0 && report.IgnoredCount == 1, "Repetir residência mesclada passa a ignorado");
}
var auditRoundtrip = JsonSerializer.Deserialize<PatientImportResultDto>(history.Last().ReportJson)!;
Check(auditRoundtrip.Entries.Single().Status == "Ignorado", "Resultado estruturado da residência é compatível com leitor do histórico");

var dbPath = Path.Combine(Path.GetTempPath(), $"acs-cep-audit-{Guid.NewGuid():N}.db");
var cacheConnection = new SQLite.SQLiteAsyncConnection(dbPath);
try
{
    await cacheConnection.CreateTableAsync<CepAddressCache>();
    await cacheConnection.InsertAsync(new CepAddressCache { Cep = "01001000", Rua = "Do Cache", Bairro = "Centro", Cidade = "São Paulo", Estado = "SP" });
    var dbService = Proxy.For<IDatabaseService>((m, _) => m == "get_Connection" ? cacheConnection : Task.CompletedTask);
    var handler = new CepHttpHandler();
    using var http = new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br/") };
    var cachedService = new ACS_View.Infrastructure.Services.CachedCepService(dbService, new ACS_View.Infrastructure.Services.ViaCepService(http));
    Check((await cachedService.GetAddressByCepAsync("01001-000"))?.Rua == "Do Cache" && handler.Calls == 0, "CEP conhecido no cache persistente não consulta a API");
    var resolved = await cachedService.GetAddressByCepAsync("22222-222");
    Check(resolved?.Rua == "Remota" && handler.Calls == 1 && await cacheConnection.Table<CepAddressCache>().CountAsync() == 2, "CEP desconhecido consulta API e armazena o resultado no SQLite");
    var reopenedService = new ACS_View.Infrastructure.Services.CachedCepService(dbService, new ACS_View.Infrastructure.Services.ViaCepService(http));
    Check((await reopenedService.GetAddressByCepAsync("22222222"))?.Rua == "Remota" && handler.Calls == 1, "Outra instância reutiliza cache persistente sem repetir chamada de API");
}
finally
{
    await cacheConnection.CloseAsync();
    File.Delete(dbPath);
}
Console.WriteLine("Audit and CEP cache regression checks passed.");

// Exercise the real PatientService normalization boundary (the basic proxy above does not normalize names).
var storedPatients = new List<Patient>
{
    new() { Id = 600, Name = "João Oliveira", MotherName = "Maria Oliveira", FatherName = "Pedro Oliveira",
        SusNumber = "910", BirthDate = new DateTime(2000, 2, 3, 14, 30, 0), Sexo = "Masculino",
        Observacao = "Observação A | Observação B", IsActive = false, StatusReason = "Registro manual",
        RowRevision = 12, SearchName = "joao oliveira", FamilyResponsibleSus = "123456789012345",
        SusNumberRecords = [new() { Number = "910", IsPrimary = true }, new() { Number = "911", OriginalOrder = 1 }] }
};
Patient CopyStoredPatient(Patient source)
{
    var copy = Clone(source);
    copy.SusNumberRecords = source.SusNumberRecords?.Select(Clone).ToArray();
    return copy;
}
var patientWrites = 0;
var normalizedRepository = Proxy.For<IPatientRepository>((method, args) => method switch
{
    "GetAllAsync" => Task.FromResult<List<Patient>?>(storedPatients.Select(CopyStoredPatient).ToList()),
    "UpdateAsync" => StoreNormalizedPatient((Patient)args[0]!),
    _ => throw new Exception(method)
});
Task StoreNormalizedPatient(Patient value)
{
    patientWrites++;
    storedPatients.RemoveAll(p => p.Id == value.Id);
    storedPatients.Add(CopyStoredPatient(value));
    return Task.CompletedTask;
}
var normalizedPatientService = new PatientService(normalizedRepository);
var normalizedImporter = new PatientImportService(normalizedPatientService, cep, conditionRepo, benefits, new SpreadsheetReader(),
    new PatientFamilyLinkResolver(normalizedPatientService, houseService, family), journal);
async Task<PatientImportResultDto> ImportNormalized(string[][] rows, bool resolveLinks = false)
{
    using var book = Workbook(rows);
    return await normalizedImporter.ImportAsync(book, new() { EnableAutomaticFamilyLinking = resolveLinks });
}
var normalizedRepeat = await ImportNormalized([
    ["nome", "cns", "nome da mae", "nome do pai", "data nascimento", "sexo", "observacao", "cns do responsavel", "coluna não usada"],
    ["JOÃO  OLIVEIRA", "910", "MARIA OLIVEIRA", "PEDRO OLIVEIRA", "03/02/2000", "M", "Observação A | Observação B", "123.4567.8901.2345", "dado externo"]
]);
Check(normalizedRepeat.MergedCount == 0 && normalizedRepeat.IgnoredCount == 1 && patientWrites == 0,
    "Serviço real: maiúsculas, espaços, horário no nascimento, observações já presentes e CNS formatado não geram mesclagem nem gravação");
var sparseRepeat = await ImportNormalized([["nome", "cns", "campo externo"], ["João Oliveira", "910", "qualquer valor"]]);
Check(sparseRepeat.IgnoredCount == 1 && sparseRepeat.MergedCount == 0 && patientWrites == 0 && storedPatients[0].StatusReason == "Registro manual",
    "Dados adicionais do app, campos ausentes e colunas não utilizadas não contam como diferença");
var relationalRepeat = await ImportNormalized([["nome", "cns"], ["João Oliveira", "911"]]);
Check(relationalRepeat.IgnoredCount == 1 && relationalRepeat.MergedCount == 0 && patientWrites == 0 && storedPatients[0].SusNumbers.SequenceEqual(["910", "911"]),
    "CNS secundário já existente apenas na coleção relacional não é tratado como informação nova");
var realDifference = await ImportNormalized([["nome", "cns", "observacao"], ["João Oliveira", "910", "Observação C"]]);
Check(realDifference.MergedCount == 1 && patientWrites == 1 && realDifference.Entries.Single().Changes.Contains("Observação C"),
    "Informação realmente nova em campo utilizado gera uma mesclagem com antes e depois");
var realDifferenceRepeat = await ImportNormalized([["nome", "cns", "observacao"], ["JOÃO OLIVEIRA", "910", "Observação C"]]);
Check(realDifferenceRepeat.IgnoredCount == 1 && realDifferenceRepeat.MergedCount == 0 && patientWrites == 1,
    "Repetir a informação recém-mesclada volta a ignorado sem nova gravação");
storedPatients.Add(new() { Id = 601, Name = "Maria Oliveira", SusNumber = "920", BirthDate = new DateTime(1970, 1, 1) });
var automaticLink = await ImportNormalized([["nome", "cns"], ["João Oliveira", "910"]], resolveLinks: true);
Check(automaticLink.IgnoredCount == 1 && automaticLink.MergedCount == 0 && storedPatients.Single(p => p.Id == 600).MotherPatientId == 601 &&
    automaticLink.Entries.Any(e => e.Status == "Vínculo ajustado"),
    "Vínculo inferido com dados já existentes é auditado separadamente e não transforma linha sem novidades em mesclada");
Console.WriteLine("Normalized patient comparison checks passed.");

await FamilyResponsibleImportTests.RunAsync();

static MemoryStream Workbook(string[][] rows)
{
    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    var stream = new MemoryStream();
    using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
    using (var writer = new StreamWriter(zip.CreateEntry("xl/worksheets/sheet1.xml").Open()))
        writer.Write(new XDocument(new XElement(ns + "worksheet", new XElement(ns + "sheetData", rows.Select((r, i) =>
            new XElement(ns + "row", new XAttribute("r", i+1), r.Select(v => new XElement(ns + "c", new XAttribute("t", "inlineStr"), new XElement(ns + "is", new XElement(ns + "t", v))))))))));
    stream.Position = 0;
    return stream;
}

public class Proxy : DispatchProxy
{
    public Func<string, object?[], object?> Handler = null!;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!.Name, args ?? []);
    public static T For<T>(Func<string, object?[], object?> handler) where T : class
    {
        var proxy = Create<T, Proxy>(); ((Proxy)(object)proxy).Handler = handler; return proxy;
    }
}

public sealed class CepHttpHandler : HttpMessageHandler
{
    public int Calls { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"cep\":\"22222-222\",\"logradouro\":\"Rua Remota\",\"bairro\":\"Centro\",\"localidade\":\"São Paulo\",\"uf\":\"SP\"}")
        });
    }
}
