using System.Text;
using System.Text.Json;
using ACS_View.Application.Backups;
using ACS_View.Application.Security;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Infrastructure.Data.Migrations;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

SQLitePCL.Batteries_V2.Init();
var checks = 0;
var directory = Path.Combine(Directory.GetCurrentDirectory(), "tmp", "acs-backups-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
const string loginPassword = "Conta de teste segura!";
const string backupPassword = "Backup de teste seguro!";
await Reject(()=>Task.Run(()=>BackupEncryptionService.ValidatePassword(new string(' ',12))),"Senha de backup formada apenas por espaços recusada");
var source = new SQLiteAsyncConnection(Path.Combine(directory, "source.db"));
var db = new DatabaseService(source); await db.InitializeAsync();
var context = new CurrentUserContext();
var auth = new AuthService(db, context, SecureStorage.Default, TimeProvider.System, new NoBiometric());
foreach (var name in new[] { "perfil-A", "perfil-B", "perfil-C" }) await auth.RegisterAsync(name, loginPassword);
var users = await source.Table<User>().ToListAsync();
foreach (var u in users) await Populate(source, u);
await source.InsertAsync(new Patient { UserId = 0, Name = "legado sem proprietário" });
await auth.LoginAsync("perfil-B", loginPassword);
Preferences.Set($"DashboardMetricPreferences.{context.CurrentUserId}", "{\"generalOrder\":[\"patients\"]}");
var service = new EncryptedBackupService(db, new DatabaseBackupService(db), context, auth);
using var archive = new MemoryStream();
await service.ExportAsync(archive, loginPassword, backupPassword, default);
Check(!Encoding.UTF8.GetString(archive.ToArray()).Contains("perfil-B"), "Identidade permanece criptografada");
archive.Position = 0; var document = await BackupEncryptionService.DecryptAsync(archive, backupPassword);
UserBackupData.Validate(document);
Check(document.Tables["PatientInfectiousFollowUp"].Count==3,"TB, HIV and immunodeficiency exported only for the selected profile");
Check(document.Tables["User"].Count == 1, "Exatamente uma conta recuperável");
Check(document.Tables["PatientGlucoseMonitoring"].Count == 1 && document.Tables["PatientStripPickup"].Count == 1, "Monitoramento e retiradas exportados somente do perfil selecionado");
var json = JsonSerializer.Serialize(document);
Check(json.Contains("paciente perfil-B") && !json.Contains("paciente perfil-A") && !json.Contains("paciente perfil-C") && !json.Contains("legado sem proprietário"), "Isolamento dos três perfis e legado UserId=0");
Check(document.Tables["PatientSusNumber"].Count == 1 && document.Tables["FamilyMember"].Count == 1 && document.Tables["FamilyResidenceHistory"].Count == 1, "Dependências indiretas e históricos preservados");
Check(document.Tables["User"][0]["BiometricTokenHash"].GetString() == "", "Nenhum token biométrico exportado");
Check(document.DashboardPreferences.Contains("patients"), "Preferências persistentes do perfil exportadas");
Check(document.Tables["VisitCareLine"].Count == 1 && document.Tables["PatientVaccineDose"].Count == 1 && document.Tables["TerritorialAssignment"].Count == 1, "Linhas, doses e território preservados");

// Merge into existing A/B/C with colliding IDs, using another profile name (D).
var imported = Clone(document); imported.Tables["User"][0]["Username"] = JsonSerializer.SerializeToElement("perfil-D");
var destination = new SQLiteAsyncConnection(Path.Combine(directory, "destination.db"));
await new DatabaseService(destination).InitializeAsync();
await destination.ExecuteAsync("INSERT INTO CidSubcategory(Id,Code,Description,CategoryCode) VALUES(17,'B00.0','Catálogo local','B00')");
foreach (var u in users)
{
    var copy = new User { Username = u.Username }; AccountSecurity.SetPassword(copy, loginPassword); await destination.InsertAsync(copy); await Populate(destination, copy);
}
await destination.ExecuteAsync("INSERT INTO Vaccines(Id,UserId) VALUES(500,0)");
await destination.ExecuteAsync("UPDATE sqlite_sequence SET seq=700 WHERE name='Note'");
List<SchemaMigrator.TableSnapshot>? before = null;
await destination.RunInTransactionAsync(c =>
{
    before = SchemaMigrator.Capture(c);
    UserBackupData.Merge(c, imported, null, default);
    SchemaMigrator.ValidatePreservation(c, before);
});
Check(await destination.Table<User>().CountAsync() == 4, "D acrescentado sem modificar A/B/C");
var d = await destination.Table<User>().FirstAsync(u => u.Username == "perfil-D");
var patientD = await destination.Table<Patient>().FirstAsync(p => p.UserId == d.Id);
Check(patientD.Id != document.Tables["Patient"][0]["Id"].GetInt32(), "Colisão de ID de paciente remapeada");
Check(patientD.Id > 500, "ID compartilhado de vacina legada órfã permanece reservado");
Check((await destination.Table<Note>().FirstAsync(n => n.UserId == d.Id)).Id > 700, "Sequência autoincremental evita reutilizar IDs antigos de lembretes");

var restoredMonitoring = await destination.Table<PatientGlucoseMonitoring>().FirstAsync(v => v.UserId == d.Id);
var restoredPickup = await destination.Table<PatientStripPickup>().FirstAsync(v => v.UserId == d.Id);
Check(restoredMonitoring.PatientId == patientD.Id && restoredMonitoring.ChecksPerDay == 4 && restoredPickup.PatientId == patientD.Id && restoredPickup.Quantity == 100, "Monitoramento e histórico remapeados para o paciente e perfil restaurados");
var restoredInfectious = await destination.Table<PatientInfectiousFollowUp>().Where(v=>v.UserId==d.Id).ToListAsync();
Check(restoredInfectious.Count == 3 && restoredInfectious.All(v => v.PatientId == patientD.Id)
    && restoredInfectious.Any(v => v.ConditionCode == "immunodeficiency" && v.Notes == "Synthetic immunodeficiency"),
    "TB, HIV and immunodeficiency restore remaps patient and profile identities");
var visit = await destination.Table<Visits>().FirstAsync(v => v.UserId == d.Id);
var batch = await destination.FindAsync<VisitBatch>(visit.BatchId);
Check(visit.PatientId == patientD.Id && batch.UserId == d.Id && visit.HouseId == patientD.HouseId, "Referências de visita, lote e residência remapeadas");
var member = await destination.Table<FamilyMember>().FirstAsync(m => m.UserId == d.Id);
Check(member.PatientId == patientD.Id && (await destination.FindAsync<FamilyRecord>(member.FamilyId)).UserId == d.Id, "Famílias relacionais remapeadas");
Check(patientD.FamilyId == 7, "Número local familiar preservado, sem confundir com ID relacional");
var notification = await destination.Table<CareNotification>().FirstAsync(n => n.UserId == d.Id);
var pregnancy = await destination.Table<PatientPregnancy>().FirstAsync(n => n.UserId == d.Id);
Check(notification.UniqueKey == $"pregnancy:missing-data:{patientD.Id}:{pregnancy.Id}", "Identificadores derivados remapeados");
var history = await destination.Table<ImportHistory>().FirstAsync(h => h.UserId == d.Id);
Check(JsonDocument.Parse(history.ReportJson).RootElement.GetProperty("Entries")[0].GetProperty("RecordId").GetInt32() == patientD.Id, "Referência JSON da auditoria remapeada");
var cid = await destination.Table<PatientCID>().FirstAsync(p => p.UserId == d.Id);
Check(cid.CidId != 17 && (await destination.FindAsync<CidSubcategory>(cid.CidId)).Code == "A00.0", "Catálogo CID remapeado por código sem sobrescrever catálogo local");
foreach (var table in UserBackupData.OwnedTables.Where(t => t != "User"))
    Check(await destination.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM \"{table}\" WHERE UserId=?", d.Id) == document.Tables[table].Count, "Contagem integral de " + table);
await Reject(async () => await destination.RunInTransactionAsync(c => UserBackupData.Merge(c, document, null, default)), "Conflito de nome recusa sobrescrita");
using (var c = new SQLiteConnection(destination.DatabasePath)) SchemaMigrator.ValidateIntegrity(c);

// Clean restore and real original-password authentication.
var clean = new SQLiteAsyncConnection(Path.Combine(directory, "health_app.db")); var cleanDb = new DatabaseService(clean); await cleanDb.InitializeAsync();
await clean.RunInTransactionAsync(c => UserBackupData.Merge(c, document, null, default));
var cleanContext = new CurrentUserContext();
var cleanAuth = new AuthService(cleanDb, cleanContext, new MemoryStorage(), TimeProvider.System, new NoBiometric());
await cleanAuth.LoginAsync("perfil-B", loginPassword);
Check(cleanContext.HasCurrentUser, "Instalação limpa autentica com a senha original");
Check((await clean.Table<User>().FirstAsync()).RecoveryCodeHash == document.Tables["User"][0]["RecoveryCodeHash"].GetString(), "Hash e salt de recuperação preservados");
Check((await clean.Table<User>().FirstAsync()).BiometricTokenHash == "", "Vínculo biométrico removido na restauração");

await Reject(async () => { using var s = new MemoryStream(archive.ToArray()); await BackupEncryptionService.DecryptAsync(s, "senha incorreta"); }, "Senha incorreta");
foreach (var length in new[] { 0, 8, 51, archive.ToArray().Length - 1 })
    await Reject(async () => { using var s = new MemoryStream(archive.ToArray()[..length]); await BackupEncryptionService.DecryptAsync(s, backupPassword); }, "Arquivo truncado " + length);
var tampered = archive.ToArray(); tampered[^20] ^= 1;
await Reject(async () => { using var s = new MemoryStream(tampered); await BackupEncryptionService.DecryptAsync(s, backupPassword); }, "Arquivo adulterado");
tampered = archive.ToArray(); tampered[8] = 255;
await Reject(async () => { using var s = new MemoryStream(tampered); await BackupEncryptionService.DecryptAsync(s, backupPassword); }, "Versão futura");
tampered = archive.ToArray(); tampered[0] = 0;
await Reject(async () => { using var s = new MemoryStream(tampered); await BackupEncryptionService.DecryptAsync(s, backupPassword); }, "Outro formato");
await Reject(async () => { using var s = new MemoryStream(archive.ToArray().Concat(new byte[1]).ToArray()); await BackupEncryptionService.DecryptAsync(s, backupPassword); }, "Conteúdo adicional");
var malicious = Clone(document); malicious.Tables["Patient"][0]["UserId"] = JsonSerializer.SerializeToElement(999);
await Reject(() => Task.Run(() => UserBackupData.Validate(malicious)), "Dado de outro usuário");
malicious = Clone(document); malicious.Tables["Patient"][0]["FatherPatientId"] = JsonSerializer.SerializeToElement(999);
await Reject(() => Task.Run(() => UserBackupData.Validate(malicious)), "Referência indireta externa");
malicious = Clone(document); malicious.Tables["CareNotification"][0]["MetadataJson"] = JsonSerializer.SerializeToElement("{\"PatientId\":999}");
await Reject(() => Task.Run(() => UserBackupData.Validate(malicious)), "JSON opaco não classificado");
malicious = Clone(document); malicious.Tables["other"] = [];
await Reject(() => Task.Run(() => UserBackupData.Validate(malicious)), "Tabela não permitida");
malicious = Clone(document); malicious.Tables["Patient"][0]["OtherUserId"] = JsonSerializer.SerializeToElement(999);
await Reject(() => Task.Run(() => UserBackupData.Validate(malicious)), "Coluna indireta não classificada recusada");
malicious = Clone(document); malicious.Tables["User"][0]["PasswordHashVersion"] = JsonSerializer.SerializeToElement(99);
await Reject(() => Task.Run(() => UserBackupData.Validate(malicious)), "Hash de autenticação incompatível recusado antes da restauração");
await Reject(() => Task.Run(() => DatabaseRecovery.EnsureStorage(directory, 1000, _ => 0)), "Falta de armazenamento preserva original");
using (var cts = new CancellationTokenSource())
{
    var interrupted = Clone(imported); interrupted.Tables["User"][0]["Username"] = JsonSerializer.SerializeToElement("perfil-E");
    cts.Cancel(); await Reject(async () => await destination.RunInTransactionAsync(c => UserBackupData.Merge(c, interrupted, null, cts.Token)), "Interrupção da preparação reverte transação");
    Check(await destination.Table<User>().CountAsync() == 4, "Cancelamento não deixa inserções parciais");
}
var invalidLateRow = Clone(imported); invalidLateRow.Tables["User"][0]["Username"] = JsonSerializer.SerializeToElement("perfil-falha");
invalidLateRow.Tables["PatientPhone"][0]["Number"] = JsonSerializer.SerializeToElement("");
await Reject(async () => await destination.RunInTransactionAsync(c => UserBackupData.Merge(c, invalidLateRow, null, default)), "Falha após inserir conta, casas e pacientes reverte toda a importação");
Check(await destination.Table<User>().CountAsync() == 4 && await destination.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='trigger'") > 0, "Rollback preserva contas e triggers de integridade");

var large = Clone(document); large.Tables["Note"][0]["Content"] = JsonSerializer.SerializeToElement(new string('x', 200_000));
using var blocks = new MemoryStream(); await BackupEncryptionService.EncryptAsync(large, blocks, backupPassword); blocks.Position = 0;
Check((await BackupEncryptionService.DecryptAsync(blocks, backupPassword)).Tables["Note"][0]["Content"].GetString()!.Length == 200_000, "Arquivo com múltiplos blocos preserva conteúdo");
var reordered = blocks.ToArray(); int blockSize = BackupEncryptionService.ChunkBytes + 16;
var firstBlock = reordered.AsSpan(52, blockSize).ToArray(); reordered.AsSpan(52 + blockSize, blockSize).CopyTo(reordered.AsSpan(52, blockSize)); firstBlock.CopyTo(reordered, 52 + blockSize);
await Reject(async () => { using var s = new MemoryStream(reordered); await BackupEncryptionService.DecryptAsync(s, backupPassword); }, "Reordenação de blocos autenticados");
var oversized = blocks.ToArray(); System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(oversized.AsSpan(40), long.MaxValue);
await Reject(async () => { using var s = new MemoryStream(oversized); await BackupEncryptionService.DecryptAsync(s, backupPassword); }, "Tamanho malicioso rejeitado antes da derivação de chave");
var previousSchema = Clone(document); previousSchema.SchemaVersion = 4;
previousSchema.Tables["User"][0]["Username"] = JsonSerializer.SerializeToElement("perfil-antigo");
foreach (var h in previousSchema.Tables["House"]) { h.Remove("SetorCensitario"); h.Remove("NumeroQuadra"); }
int oldId = 0; await destination.RunInTransactionAsync(c => oldId = UserBackupData.Merge(c, previousSchema, null, default));
Check((await destination.Table<House>().FirstAsync(h => h.UserId == oldId)).SetorCensitario == "01", "Backup do esquema 4 aplica transformação territorial do esquema 5");
await auth.LogoutAsync();
await Reject(async () => { using var output = new MemoryStream(); await service.ExportAsync(output, loginPassword, backupPassword, default); }, "Exportação sem sessão");
archive.Position = 0; await service.InspectAsync(archive, backupPassword, default);
await Reject(() => service.RestoreAsync(true, loginPassword, default), "Substituição sem autorização");
Check(!File.Exists(Path.Combine(directory, DatabaseRecovery.SelectionFile)), "Falhas não ativam outro banco");

// Authorized full replacement and preservation of the two other accounts.
var replacementDirectory = Path.Combine(directory, "replace"); Directory.CreateDirectory(replacementDirectory);
var replacementPath = Path.Combine(replacementDirectory, "health_app.db");
var sourceSnapshot = await DatabaseRecovery.CreateSnapshotAsync(source);
DatabaseRecovery.RestoreToNewPath(sourceSnapshot, replacementPath);
var replacementConnection = new SQLiteAsyncConnection(replacementPath); var replacementDb = new DatabaseService(replacementConnection); await replacementDb.InitializeAsync();
var replacementContext = new CurrentUserContext(); var replacementAuth = new AuthService(replacementDb, replacementContext, new MemoryStorage(), TimeProvider.System, new NoBiometric());
await replacementAuth.LoginAsync("perfil-B", loginPassword);
var replacementService = new EncryptedBackupService(replacementDb, new DatabaseBackupService(replacementDb), replacementContext, replacementAuth);
archive.Position = 0; await replacementService.InspectAsync(archive, backupPassword, default);
await Reject(() => replacementService.RestoreAsync(true, "senha errada", default), "Substituição exige senha atual correta");
await replacementService.RestoreAsync(true, loginPassword, default);
var replacementSelected = DatabaseRecovery.GetSelectedDatabasePath(replacementDirectory);
using (var c = new SQLiteConnection(replacementSelected))
using (var original = new SQLiteConnection(sourceSnapshot))
{
    Check(c.ExecuteScalar<int>("SELECT COUNT(*) FROM User") == 3, "Substituição mantém exatamente A/B/C");
    foreach (var id in new[] { users[0].Id, users[2].Id })
        Check(JsonSerializer.Serialize(UserBackupData.Extract(c, id, "test", "").Tables) == JsonSerializer.Serialize(UserBackupData.Extract(original, id, "test", "").Tables), "Substituição preserva integralmente perfil " + id);
}
await replacementConnection.CloseAsync();

// A damaged prepared file must automatically select the intact prior DB.
var rollbackDirectory = Path.Combine(directory, "rollback"); Directory.CreateDirectory(rollbackDirectory);
var rollbackOriginal = Path.Combine(rollbackDirectory, "health_app.db"); DatabaseRecovery.RestoreToNewPath(sourceSnapshot, rollbackOriginal);
var broken = Path.Combine(rollbackDirectory, "health_app-restored-broken.db"); DatabaseRecovery.RestoreToNewPath(sourceSnapshot, broken);
DatabaseRecovery.SelectProfileBackupForNextStart(rollbackDirectory, broken, rollbackOriginal);
File.WriteAllBytes(broken, [1, 2, 3]);
Check(DatabaseRecovery.GetSelectedDatabasePath(rollbackDirectory) == rollbackOriginal, "Falha de ativação recupera automaticamente o banco anterior");
Check(File.Exists(Path.Combine(rollbackDirectory, "profile-backup-activation.json.failed")), "Plano de recuperação da falha mantido para diagnóstico");
var validCandidate = Path.Combine(rollbackDirectory, "health_app-restored-torn.db"); DatabaseRecovery.RestoreToNewPath(sourceSnapshot, validCandidate);
DatabaseRecovery.SelectProfileBackupForNextStart(rollbackDirectory, validCandidate, rollbackOriginal);
File.WriteAllText(Path.Combine(rollbackDirectory, DatabaseRecovery.SelectionFile), "");
Check(DatabaseRecovery.GetSelectedDatabasePath(rollbackDirectory) == rollbackOriginal, "Seleção interrompida recupera caminho anterior pelo diário");

// End-to-end public restore: staging, activation pointer, frozen old connection, next start.
var cleanService = new EncryptedBackupService(cleanDb, new DatabaseBackupService(cleanDb), cleanContext, cleanAuth);
Preferences.Set("DashboardMetricPreferences.1", "configuração preexistente");
using var second = new MemoryStream(); await BackupEncryptionService.EncryptAsync(imported, second, backupPassword); second.Position = 0;
await cleanService.InspectAsync(second, backupPassword, default);
await cleanService.RestoreAsync(false, "", default);
Check(await clean.Table<User>().CountAsync() == 1, "Banco ativo não substituído durante a preparação");
Check(await clean.ExecuteScalarAsync<int>("PRAGMA query_only") == 1, "Escritas bloqueadas depois de preparar ativação");
second.Position = 0; await cleanService.InspectAsync(second, backupPassword, default);
await Reject(() => cleanService.RestoreAsync(false, "", default), "Segunda restauração antes do reinício recusada");
Check(await clean.ExecuteScalarAsync<int>("PRAGMA query_only") == 1, "Segunda tentativa não libera escritas no banco antigo");
var selected = DatabaseRecovery.GetSelectedDatabasePath(directory);
var restarted = new SQLiteAsyncConnection(selected); await new DatabaseService(restarted).InitializeAsync();
EncryptedBackupService.ApplyRestoredSettings(selected);
Check(await restarted.Table<User>().CountAsync() == 2, "Próximo início ativa banco consolidado");
Check(File.Exists(clean.DatabasePath), "Banco anterior mantido para recuperação");
Check(!cleanContext.HasCurrentUser, "Sessão não transportada");
Check(Preferences.Get("BackupRequirePasswordOnce", "") == "1", "Primeiro acesso restaurado exige senha sem tentativa biométrica automática");
Check(Preferences.Get("DashboardMetricPreferences.1", "") == "configuração preexistente" && Preferences.Get("DashboardMetricPreferences.2", "") == imported.DashboardPreferences, "Restauração aplica configurações somente ao novo perfil");

// Kinship backup v2, profile isolation, ID collisions and legacy v1 compatibility.
var kinshipPerson=new Patient{UserId=users[1].Id,Name="Parente sintético"};await source.InsertAsync(kinshipPerson);
var kinshipOwner=users[1].Id;
var sourcePerson=await source.Table<Patient>().FirstAsync(p=>p.UserId==kinshipOwner && p.Id!=kinshipPerson.Id);
await source.InsertAsync(new FamilyRelationship{UserId=users[1].Id,FromPatientId=sourcePerson.Id,ToPatientId=kinshipPerson.Id,TypeCode="custom",ForwardLabel="Tutor",ReverseLabel="Tutoreado",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow});
BackupDocument kinshipDocument;
using(var c=new SQLiteConnection(source.DatabasePath))kinshipDocument=UserBackupData.Extract(c,users[1].Id,"test","");
Check(kinshipDocument.FormatVersion==2 && kinshipDocument.Tables["FamilyRelationship"].Count==1,"Backup v2 includes only current profile's relationships");
kinshipDocument.Tables["User"][0]["Username"]=JsonSerializer.SerializeToElement("perfil-parentesco");
int importedKinshipUser=0;await destination.RunInTransactionAsync(c=>importedKinshipUser=UserBackupData.Merge(c,kinshipDocument,null,default));
var kinshipLink=await destination.Table<FamilyRelationship>().FirstAsync(r=>r.UserId==importedKinshipUser);
Check(kinshipLink.FromPatientId!=sourcePerson.Id && kinshipLink.ToPatientId!=kinshipPerson.Id && (await destination.FindAsync<Patient>(kinshipLink.FromPatientId)).UserId==importedKinshipUser && (await destination.FindAsync<Patient>(kinshipLink.ToPatientId)).UserId==importedKinshipUser,"Restore remaps both kinship endpoints within destination profile");
var restoredKinshipPeople=(await destination.Table<Patient>().Where(p=>p.UserId==importedKinshipUser).ToListAsync()).ToDictionary(p=>p.Id);
var restoredTutor=restoredKinshipPeople.Values.Single(p=>p.Name==sourcePerson.Name);var restoredWard=restoredKinshipPeople.Values.Single(p=>p.Name==kinshipPerson.Name);
var restoredKinship=new ACS_View.Domain.ValueObjects.KinshipSnapshot(importedKinshipUser,restoredKinshipPeople,[kinshipLink]);
Check(ACS_View.Domain.ValueObjects.KinshipEngine.Query(restoredKinship,restoredTutor.Id).Single().Label=="Tutoreado" && ACS_View.Domain.ValueObjects.KinshipEngine.Query(restoredKinship,restoredWard.Id).Single().Label=="Tutor","Custom reciprocal meanings survive restore even when endpoint ordering changes");
Check(kinshipLink.FromPatientId<kinshipLink.ToPatientId,"Restore renormalizes relationship endpoints after ID mapping");
var previousInfectiousFormat=Clone(document);previousInfectiousFormat.SchemaVersion=7;previousInfectiousFormat.Tables.Remove("PatientInfectiousFollowUp");UserBackupData.Validate(previousInfectiousFormat);
Check(previousInfectiousFormat.Tables["PatientInfectiousFollowUp"].Count==0,"Backups before infectious follow-up remain compatible");
var previousHealthFormat = Clone(document); previousHealthFormat.SchemaVersion = 6;
previousHealthFormat.Tables.Remove("PatientGlucoseMonitoring"); previousHealthFormat.Tables.Remove("PatientStripPickup");
UserBackupData.Validate(previousHealthFormat);
Check(previousHealthFormat.Tables["PatientGlucoseMonitoring"].Count == 0 && previousHealthFormat.Tables["PatientStripPickup"].Count == 0, "Backup anterior à migração 7 permanece compatível");
var invalidHealth = Clone(document); invalidHealth.Tables["PatientStripPickup"][0]["PatientId"] = JsonSerializer.SerializeToElement(2147483647);
await Reject(() => Task.Run(() => UserBackupData.Validate(invalidHealth)), "Retirada com paciente externo rejeitada na validação do backup");
var oldFormat=Clone(document);oldFormat.FormatVersion=1;oldFormat.SchemaVersion=5;oldFormat.Tables.Remove("FamilyRelationship");UserBackupData.Validate(oldFormat);
Check(oldFormat.Tables["FamilyRelationship"].Count==0,"Old v1 backups remain readable without fabricated relationships");
var contaminated=Clone(kinshipDocument);contaminated.Tables["FamilyRelationship"][0]["ToPatientId"]=JsonSerializer.SerializeToElement(2147483647);
await Reject(()=>Task.Run(()=>UserBackupData.Validate(contaminated)),"Backup with external kinship reference rejected");
await restarted.CloseAsync(); await source.CloseAsync(); await destination.CloseAsync(); await clean.CloseAsync();
Console.WriteLine($"{checks} backup integration checks passed.");

void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; Console.WriteLine("PASS: " + message); }
async Task Reject(Func<Task> action, string message)
{
    try { await action(); } catch (Exception e) when (e is InvalidOperationException or IOException or InvalidDataException or SQLiteException or OperationCanceledException or JsonException) { Check(true, message); return; }
    throw new Exception("Expected failure: " + message);
}
BackupDocument Clone(BackupDocument d) => JsonSerializer.Deserialize<BackupDocument>(JsonSerializer.Serialize(d))!;
async Task Populate(SQLiteAsyncConnection c, User u)
{
    var house = new House { UserId = u.Id, Rua = "rua " + u.Username }; await c.InsertAsync(house);
    var p = new Patient { UserId = u.Id, Name = "paciente " + u.Username, HouseId = house.CasaId, FamilyId = 7, SusNumber = "" }; await c.InsertAsync(p);
    await c.RunInTransactionAsync(s => RelationalStore.SyncPatient(s, p));
    await c.InsertAsync(new PatientSusNumber { UserId = u.Id, PatientId = p.Id, Number = "CNS-" + u.Id, IsPrimary = true });
    await c.InsertAsync(new PatientGlucoseMonitoring { UserId = u.Id, PatientId = p.Id, ChecksPerDay = 4, StripsPerPickup = 100, PickupIntervalDays = 30 });
    await c.InsertAsync(new PatientInfectiousFollowUp { UserId=u.Id, PatientId=p.Id, ConditionCode="tuberculosis", Status="Em tratamento", Notes="Synthetic TB" });
    await c.InsertAsync(new PatientInfectiousFollowUp { UserId=u.Id, PatientId=p.Id, ConditionCode="hiv", Status="Em acompanhamento", ReferenceService="Synthetic HIV service" });
    await c.InsertAsync(new PatientInfectiousFollowUp { UserId=u.Id, PatientId=p.Id, ConditionCode="immunodeficiency", Status="Em acompanhamento", Notes="Synthetic immunodeficiency" });
    await c.InsertAsync(new PatientStripPickup { UserId = u.Id, PatientId = p.Id, PickupDate = DateTime.Today.AddDays(-1), Quantity = 100, Notes = "Retirada sintética" });
    await c.InsertAsync(new PatientPhone { UserId = u.Id, PatientId = p.Id, Number = "11999999999", Type = "Celular" });
    await c.InsertAsync(new Family { UserId = u.Id, IdFamilia = p.Id, IdPessoa = p.Id });
    await c.InsertAsync(new Vaccines { Id = p.Id, UserId = u.Id, BCG_Infantil = true });
    await c.ExecuteAsync("INSERT OR IGNORE INTO CidSubcategory(Id,Code,Description,CategoryCode) VALUES(17,'A00.0','Catálogo origem','A00')");
    await c.InsertAsync(new PatientCID { UserId = u.Id, PatientId = p.Id, CidId = 17 });
    await c.InsertAsync(new PatientConditions { UserId = u.Id, PatientId = p.Id, Description = "Diabetes" });
    await c.InsertAsync(new PatientBolsaFamilia { UserId = u.Id, PatientId = p.Id, ResponsiblePatientId = p.Id, NisNumber = "NIS " + u.Id });
    await c.InsertAsync(new PatientInsulinDependency { UserId = u.Id, PatientId = p.Id });
    await c.InsertAsync(new MigrationIssue { UserId = u.Id, SourceTable = "Patient", SourceId = p.Id, Code = "LegacyCnsEvidence", Evidence = "CNS " + u.Id });
    var batch = new VisitBatch { UserId = u.Id, HouseId = house.CasaId, FamilyId = 7 }; await c.InsertAsync(batch);
    var visit = new Visits { UserId = u.Id, PatientId = p.Id, HouseId = house.CasaId, FamilyId = 7, BatchId = batch.Id }; await c.InsertAsync(visit);
    await c.InsertAsync(new VisitCareLine { UserId = u.Id, VisitId = visit.Id, CareLineType = "Diabetes" });
    await c.InsertAsync(new PatientVaccineDose { UserId = u.Id, PatientId = p.Id, DoseKey = "test", IsApplied = true });
    var pregnancy = new PatientPregnancy { UserId = u.Id, PatientId = p.Id }; await c.InsertAsync(pregnancy);
    await c.InsertAsync(new CareNotification { UserId = u.Id, PatientId = p.Id, UniqueKey = $"pregnancy:missing-data:{p.Id}:{pregnancy.Id}" });
    await c.InsertAsync(new Note { UserId = u.Id, Content = "nota " + u.Username, NotifyOn = DateTime.Now.AddDays(1) });
    await c.InsertAsync(new TerritorialAssignment { UserId = u.Id, HouseId = house.CasaId, CensusSector = "01", BlockNumber = "02" });
    await c.InsertAsync(new ImportHistory { UserId = u.Id, ReportJson = JsonSerializer.Serialize(new { Entries = new[] { new { EntityType = "Paciente", RecordId = p.Id } } }) });
}
