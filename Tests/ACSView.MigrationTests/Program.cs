using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Infrastructure.Data.Migrations;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

SQLitePCL.Batteries_V2.Init();
var checks=0;
var directory=Path.Combine(Path.GetTempPath(),"acs-relational-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
// Synthetic only. Keep failed fixtures in temp for investigation, never in Git.
await CleanInstall();
await StorageChecks();
await PhoneTests.Run(directory, Check);
WhatsAppPhoneTests.Run(Check);
await SusNumberTests.Run(directory, Check);
await SusEntityTests.Run(directory, Check);
await AdvancedFilterTests.Run(directory, Check);
await ConcurrentInitialization();
await InterruptedMigration();
await LegacyBooleanVaccines();
await LegacyAndRuntime();
await ImportIntegration();
Console.WriteLine($"{checks} migration and relational integration checks passed.");

void Check(bool pass,string description)
{
    if(!pass)throw new InvalidOperationException(description);
    checks++;Console.WriteLine("PASS: "+description);
}
async Task Reject(Func<Task> action,string description)
{
    try { await action(); } catch(Exception e) when(e is SQLiteException or InvalidOperationException or IOException) { Check(true,description);return; }
    throw new InvalidOperationException("Expected rejection: "+description);
}
async Task CleanInstall()
{
    var c=new SQLiteAsyncConnection(Path.Combine(directory,"clean.db"));
    await new DatabaseService(c).InitializeAsync();
    Check(await c.ExecuteScalarAsync<int>("PRAGMA user_version")==SchemaMigrator.CurrentVersion,"Clean installation schema version");
    Check(await c.ExecuteScalarAsync<int>("PRAGMA foreign_keys")==1,"Foreign keys enabled on runtime connection");
    Check(await c.Table<PatientPhone>().CountAsync()==0,"No invented phone contacts");
    Check(await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SchemaMigration")==SchemaMigrator.CurrentVersion,"Ordered migration ledger");
    await new DatabaseService(c).InitializeAsync();
    Check(await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SchemaMigration")==SchemaMigrator.CurrentVersion,"Repeated initialization is idempotent");
    await c.CloseAsync();
}
async Task StorageChecks()
{
    var path=Path.Combine(directory,"storage.db");
    var c=new SQLiteAsyncConnection(path);
    await c.ExecuteScalarAsync<string>("PRAGMA journal_mode=WAL");
    await c.ExecuteScalarAsync<int>("PRAGMA wal_autocheckpoint=0");
    await c.ExecuteAsync("CREATE TABLE Evidence(Id INTEGER PRIMARY KEY,Value BLOB)");
    await c.ExecuteAsync("INSERT INTO Evidence VALUES(1,zeroblob(6*1024*1024))");
    var logicalBytes=await c.ExecuteScalarAsync<long>("PRAGMA page_count")*await c.ExecuteScalarAsync<long>("PRAGMA page_size");
    Check(logicalBytes>new FileInfo(path).Length,"Storage budget includes committed WAL pages");
    var snapshotDirectory=Path.Combine(directory,"migration-backups");
    var snapshotsBefore=Directory.Exists(snapshotDirectory)?Directory.GetFiles(snapshotDirectory).Length:0;
    string? measured=null;
    await Reject(()=>DatabaseRecovery.CreateSnapshotAsync(c,p=>{measured=p;return logicalBytes*3-1;}),"Actual insufficient storage rejects snapshot before migration");
    Check(measured==snapshotDirectory,"Snapshot measures its destination filesystem, not the system root");
    Check(Directory.GetFiles(snapshotDirectory).Length==snapshotsBefore,"Low storage creates no partial backup");
    Check(await c.ExecuteScalarAsync<int>("PRAGMA user_version")==0 && await c.ExecuteScalarAsync<int>("SELECT length(Value) FROM Evidence WHERE Id=1")==6*1024*1024,"Low storage keeps original schema and data");
    var snapshot=await DatabaseRecovery.CreateSnapshotAsync(c,_=>logicalBytes*3);
    using(var saved=new SQLiteConnection(snapshot,SQLiteOpenFlags.ReadOnly))
        Check(saved.ExecuteScalar<int>("SELECT length(Value) FROM Evidence WHERE Id=1")==6*1024*1024,"Sufficient destination storage creates a validated backup including WAL data");
    var restoredPath=Path.Combine(directory,"storage-restored.db");
    await Reject(()=>Task.Run(()=>DatabaseRecovery.RestoreToNewPath(snapshot,restoredPath,_=>0)),"Insufficient restore storage keeps original and backup");
    Check(!File.Exists(restoredPath) && File.Exists(path) && File.Exists(snapshot),"Rejected restore does not create or replace a database");
    measured=null;
    DatabaseRecovery.RestoreToNewPath(snapshot,restoredPath,p=>{measured=p;return long.MaxValue;});
    Check(measured==directory,"Restore measures the destination directory");
    await c.CloseAsync();
}
async Task InterruptedMigration()
{
    var c=new SQLiteAsyncConnection(Path.Combine(directory,"interrupted.db"));
    await c.ExecuteAsync("CREATE TABLE Patient(Id INTEGER PRIMARY KEY,UserId INTEGER,Name TEXT,SusNumber TEXT,HouseId INTEGER,FamilyId INTEGER)");
    await c.ExecuteAsync("INSERT INTO Patient VALUES(7,0,'Synthetic','invalid;123;123',-1,-1)");
    await Reject(()=>new SchemaMigrator(c){BeforeValidation=v=>{if(v==2)throw new InvalidOperationException("Simulated interruption");}}.RunAsync(),"Interrupted transaction rolls back");
    Check(await c.ExecuteScalarAsync<int>("PRAGMA user_version")==0,"Interrupted attempt has no committed version");
    Check(await c.ExecuteScalarAsync<string>("SELECT SusNumber FROM Patient WHERE Id=7")=="invalid;123;123","Interrupted attempt preserves raw CNS");
    Check((await c.GetTableInfoAsync("PatientPhone")).Count==0,"DDL rolls back along with data");
    await Reject(()=>new SchemaMigrator(c){BeforeValidation=v=>{if(v==3)c.GetConnection().Execute("UPDATE Patient SET Name='Corrupted fixture' WHERE Id=7");}}.RunAsync(),"Preservation fingerprints reject an accidental original-row change");
    Check(await c.ExecuteScalarAsync<string>("SELECT Name FROM Patient WHERE Id=7")=="Synthetic","Fingerprint failure restores the original row");
    await new SchemaMigrator(c).RunAsync();
    Check(await c.ExecuteScalarAsync<int>("PRAGMA user_version")==SchemaMigrator.CurrentVersion,"Recovery retries successfully");
    await c.CloseAsync();
    var bad=new SQLiteAsyncConnection(Path.Combine(directory,"bad-index.db"));
    await bad.ExecuteAsync("CREATE TABLE PatientPhone(Id INTEGER PRIMARY KEY,PatientId INTEGER,IsPrimary INTEGER)");
    await bad.ExecuteAsync("INSERT INTO PatientPhone VALUES(1,1,1),(2,1,1)");
    await Reject(()=>new SchemaMigrator(bad).RunAsync(),"Constraint or partial-schema failure rejects migration without completion");
    Check(await bad.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PatientPhone")==2,"Conflicting partial rows preserved");
    await bad.CloseAsync();
    var partial=new SQLiteAsyncConnection(Path.Combine(directory,"partial.db"));
    await partial.ExecuteAsync("CREATE TABLE Patient(Id INTEGER PRIMARY KEY,UserId INTEGER,Name TEXT,SusNumber TEXT,HouseId INTEGER,FamilyId INTEGER,IsActive INTEGER)");
    await partial.ExecuteAsync("INSERT INTO Patient VALUES(1,1,'Partial','123',-1,-1,1)");
    await partial.ExecuteAsync("INSERT INTO Patient VALUES(2,NULL,'Unknown nullable owner','456',-1,-1,1)");
    await partial.ExecuteAsync("CREATE TABLE PatientConditions(Id INTEGER PRIMARY KEY,PatientId INTEGER,UserId INTEGER,Description TEXT,ConditionId INTEGER)");
    await partial.ExecuteAsync("INSERT INTO PatientConditions VALUES(1,1,1,'HIPERTENSÃO',NULL)");
    await new SchemaMigrator(partial).RunAsync();
    Check(await partial.Table<Patient>().CountAsync()==2,"Partially expanded legacy schema supported");
    Check(await partial.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PatientConditions WHERE Id=1 AND ConditionId IS NULL")==1,"Preexisting partial clinical identity remains unchanged");
    Check(await partial.Table<MigrationIssue>().CountAsync(x=>x.Code=="PendingConditionIdentity")>0,"Partial clinical identity is reported for reconciliation");
    Check(await partial.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Patient WHERE Id=2 AND UserId IS NULL")==1 && await partial.Table<PatientSusNumber>().CountAsync(x=>x.PatientId==2 && x.UserId==0)==1,"Nullable legacy ownership is preserved and quarantined without assigning an account");
    await partial.ExecuteAsync("PRAGMA user_version=100");
    await Reject(()=>new DatabaseService(partial).InitializeAsync(),"Future schema refused before writes");
    await partial.CloseAsync();
}
async Task LegacyBooleanVaccines()
{
    var c=new SQLiteAsyncConnection(Path.Combine(directory,"boolean-vaccines.db"));
    await c.ExecuteAsync("CREATE TABLE Patient(Id INTEGER PRIMARY KEY,UserId INTEGER,Name TEXT,SusNumber TEXT,HouseId INTEGER,FamilyId INTEGER)");
    await c.ExecuteAsync("INSERT INTO Patient VALUES(9,1,'Vaccination fixture','123',-1,-1)");
    await c.CreateTableAsync<Vaccines>();
    await c.InsertAsync(new Vaccines{Id=9,UserId=1,BCG_Infantil=true});
    await new SchemaMigrator(c).RunAsync();
    Check(await c.Table<PatientVaccineDose>().CountAsync()==1 && (await c.FindAsync<Vaccines>(9)).BCG_Infantil,"Legacy vaccine boolean preserved and expanded");
    Check(await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PatientVaccineDose WHERE AppliedAt IS NULL AND CreatedAt IS NULL")==1,"Unknown legacy vaccine dates are never invented");
    var context=new CurrentUserContext();context.SetCurrentUser(1);
    var vaccineService=new VaccineService(new DatabaseService(c),context);
    var schedule=await vaccineService.GetScheduleForPatientAsync(9);
    var dose=schedule!.Doses.Single(d=>d.ApplicationId!=null);
    Check(dose.IsApplied && dose.ApplicationDate==null,"Unknown application date remains unknown and the dose still displays as applied");
    var editor=new ACS_View.ViewModels.VaccineApplicationPopupViewModel(9,dose);
    editor.Notes="Additional observation";
    Check(editor.TryCreateRequest(out var noteOnly) && noteOnly!.ApplicationDate==null,"Editing an unknown-date vaccine note does not choose today's date");
    await vaccineService.ApplyDoseAsync(noteOnly!);
    Check(await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PatientVaccineDose WHERE AppliedAt IS NULL AND CreatedAt IS NULL")==1,"Unknown vaccine dates remain unknown after note editing");
    editor.HasKnownApplicationDate=true;
    Check(editor.TryCreateRequest(out var known) && known!.ApplicationDate==DateTime.Today,"Vaccine date is recorded only after explicit date-known selection");
    await c.ExecuteAsync("UPDATE PatientVaccineDose SET AppliedAt='2020-04-02 12:34:56',CreatedAt='2019-01-01 00:00:00'");
    schedule=await vaccineService.GetScheduleForPatientAsync(9);
    dose=schedule!.Doses.Single(d=>d.ApplicationId!=null);
    Check(dose.ApplicationDate==new DateTime(2020,4,2),"Legacy SQL timestamp text is read as its actual calendar date");
    await vaccineService.ApplyDoseAsync(new VaccineApplicationRequestDto(9,dose.Definition.DoseKey,new DateTime(2020,4,3),"Updated","Notes"));
    Check(await c.ExecuteScalarAsync<string>("SELECT CreatedAt FROM PatientVaccineDose")=="2019-01-01 00:00:00","Editing a vaccine preserves unrelated original creation timestamp text");
    await new SchemaMigrator(c).RunAsync();
    Check(await c.Table<PatientVaccineDose>().CountAsync()==1,"Repeated legacy vaccine initialization does not duplicate applications");
    await c.CloseAsync();
}
async Task ConcurrentInitialization()
{
    var path=Path.Combine(directory,"concurrent.db");
    var first=new SQLiteAsyncConnection(path);
    var second=new SQLiteAsyncConnection(path);
    await Task.WhenAll(new DatabaseService(first).InitializeAsync(),new DatabaseService(second).InitializeAsync());
    Check(await first.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SchemaMigration")==SchemaMigrator.CurrentVersion,"Concurrent startup commits one contiguous ledger");
    await first.CloseAsync();await second.CloseAsync();
}
async Task LegacyAndRuntime()
{
    var path=Path.Combine(directory,"legacy.db");
    var c=new SQLiteAsyncConnection(path);
    await c.ExecuteAsync("CREATE TABLE House(CasaId INTEGER PRIMARY KEY,UserId INTEGER,Rua TEXT,NumeroCasa TEXT)");
    await c.ExecuteAsync("INSERT INTO House VALUES(10,1,'Synthetic A','1'),(11,1,'Synthetic B','2'),(20,2,'Synthetic C','3')");
    await c.ExecuteAsync("CREATE TABLE Patient(Id INTEGER PRIMARY KEY,UserId INTEGER,Name TEXT,SusNumber TEXT,HouseId INTEGER,FamilyId INTEGER,Observacao TEXT)");
    var raw="123.456.789-012345; 987654321012345; 123.456.789-012345;invalid;42";
    await c.ExecuteAsync("INSERT INTO Patient VALUES(1,1,'Synthetic A',?,10,1,?),(2,1,'Synthetic B','987654321012345',10,1,'Note B'),(3,1,'Synthetic C','345',11,1,'Note C'),(4,2,'Synthetic D','456',20,1,'Note D'),(5,0,'Unowned','invalid',-1,-1,'Unowned note'),(6,1,'No house','789',-1,1,'No house'),(7,1,'No family','890',10,-1,'No family')",raw,"Long note\0preserved");
    await c.ExecuteAsync("CREATE TABLE Family(IdFamilia INTEGER PRIMARY KEY,UserId INTEGER,IdPessoa INTEGER)");
    await c.ExecuteAsync("INSERT INTO Family VALUES(1,1,99)");
    await c.ExecuteAsync("CREATE TABLE Visits(Id INTEGER PRIMARY KEY,UserId INTEGER,PatientId INTEGER,HouseId INTEGER,FamilyId INTEGER,Date BIGINT,Description TEXT,Address TEXT)");
    await c.ExecuteAsync("INSERT INTO Visits VALUES(8,1,1,10,1,?,'Realizada','Historical snapshot'),(9,1,999,999,1,?,'Ausente','Orphan snapshot')",DateTime.Today.AddYears(-3),DateTime.Today.AddYears(-4));
    await c.CreateTableAsync<PatientVaccineDose>();
    await c.ExecuteAsync("INSERT INTO PatientVaccineDose(Id,UserId,PatientId,DoseKey,IsApplied,AppliedAt,LotNumber,Notes,CreatedAt) VALUES(31,1,1,'synthetic',1,?,'A','First',?),(32,1,1,'synthetic',1,?,'B','Second',?)",DateTime.Today.AddYears(-2),DateTime.Today.AddYears(-2),DateTime.Today.AddYears(-1),DateTime.Today.AddYears(-1));
    await c.CreateTableAsync<PatientPregnancy>();
    await c.InsertAsync(new PatientPregnancy{UserId=1,PatientId=1,Status=PregnancyStatus.Active,Notes="Episode A"});
    await c.InsertAsync(new PatientPregnancy{UserId=1,PatientId=1,Notes="Episode B"});
    await c.ExecuteAsync("CREATE TABLE PatientConditions(Id INTEGER PRIMARY KEY AUTOINCREMENT,UserId INTEGER,PatientId INTEGER,Description TEXT)");
    await c.ExecuteAsync("INSERT INTO PatientConditions(UserId,PatientId,Description) VALUES(1,1,'Diabetes')");
    await c.CreateTableAsync<PatientCID>();
    await c.InsertAsync(new PatientCID{UserId=1,PatientId=1,CidId=123});
    var migrator=new SchemaMigrator(c);
    await migrator.RunAsync();
    Check(migrator.BackupPath!=null && File.Exists(migrator.BackupPath),"Consistent pre-migration backup exists");
    Check(await c.ExecuteScalarAsync<string>("SELECT SusNumber FROM Patient WHERE Id=1")==raw,"Raw invalid, formatted and duplicate CNS preserved");
    Check(await c.ExecuteScalarAsync<string>("SELECT hex(Observacao) FROM Patient WHERE Id=1")==Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes("Long note\0preserved")),"Embedded NUL and long observations preserved");
    Check(await c.Table<Patient>().CountAsync()==7,"Original IDs and count preserved");
    Check(await c.Table<PatientSusNumber>().CountAsync(s=>s.PatientId==1)==3,"CNS projection preserves every digit-bearing token under SusNumberSet rules");
    Check(await c.Table<MigrationIssue>().CountAsync(x=>x.SourceId==1 && x.Code=="CnsNeedsReview")>0,"Invalid and duplicate CNS reconciled with original evidence");
    Check(await c.Table<FamilyRecord>().CountAsync()==4,"Same family number in different houses/users remains separate; unknown house isolated");
    Check(await c.Table<FamilyMember>().CountAsync(x=>x.JoinedAt==null)==5,"Migrated membership dates remain unknown");
    Check(await c.Table<Patient>().CountAsync(p=>p.Id==5 && p.UserId==0)==1,"Unowned data retains UserId zero");
    Check(await c.Table<PatientVaccineDose>().CountAsync()==2 && (await c.FindAsync<PatientVaccineDose>(32)).LotNumber=="B","Distinct dose applications, dates and batches preserved");
    Check(await c.ExecuteScalarAsync<int>("SELECT OriginalCount FROM SchemaPreservation WHERE MigrationVersion=3 AND TableName='Patient'")==7,"Persistent preservation report includes original table counts and fingerprints");
    Check(await c.Table<PatientPregnancy>().CountAsync()==2,"Separate pregnancy episodes retained");
    Check(await c.Table<PatientConditions>().CountAsync()==1 && await c.Table<PatientCID>().CountAsync()==1,"Clinical conditions and orphan CID retained");
    Check((await c.FindAsync<Visits>(9)).Address=="Orphan snapshot","Orphan historical visit retained unchanged");
    var context=new CurrentUserContext();context.SetCurrentUser(1);
    var db=new DatabaseService(c);await db.InitializeAsync();
    await db.ClaimLegacyDataForUserAsync(1);
    Check((await c.FindAsync<Patient>(5)).UserId==0,"Login never claims unknown ownership");
    var patients=new SQLitePatientRepository(db,context);
    var houses=new SQLiteHouseRepository(db,context);
    var families=new FamilyManager(db,context);
    var contacts=new RelationalDataService(db,context);
    var pregnancyService=new PregnancyService(db,context);
    await Reject(()=>pregnancyService.SyncPregnancyConditionAsync(1,false),"Ambiguous active pregnancies require reconciliation without closing an arbitrary episode");
    await c.ExecuteAsync("UPDATE PatientPregnancy SET CreatedAt='2020-02-03 12:00:00'");
    var episode=(await pregnancyService.GetActiveOrLatestByPatientIdAsync(1))!;
    Check(episode.CreatedAt==new DateTime(2020,2,3,12,0,0),"Pregnancy reads preserve calendar semantics of old SQL timestamps");
    episode.Status=PregnancyStatus.Ended;episode.EndedAt=DateTime.Today;
    await pregnancyService.SaveAsync(episode);
    Check(await c.Table<PatientPregnancy>().CountAsync(x=>x.Status==PregnancyStatus.Active)==1,"Ending one pregnancy never changes another distinct episode");
    Check(await c.ExecuteScalarAsync<string>("SELECT CreatedAt FROM PatientPregnancy WHERE Id=?",episode.Id)=="2020-02-03 12:00:00","Clinical edits preserve original pregnancy creation timestamp text");
    var key=ACS_View.Domain.ValueObjects.VaccineDoseCatalog.LegacyBooleanColumns.First().Value;
    await c.ExecuteAsync("UPDATE PatientVaccineDose SET DoseKey=?",key);
    var vaccines=new VaccineService(db,context);
    await Reject(()=>vaccines.ApplyDoseAsync(new VaccineApplicationRequestDto(1,key,DateTime.Today,"C","Attempt")),"Vaccine edit cannot deduplicate distinct historical applications");
    await Reject(()=>vaccines.RemoveDoseApplicationAsync(1,key),"Vaccine removal requires reconciliation of multiple applications");
    var primary=new PatientPhone{PatientId=1,Number="(11) 12345-6789",IsPrimary=true};await contacts.SavePhoneAsync(primary);
    await contacts.SavePhoneAsync(new PatientPhone{PatientId=1,Number="11 98765-4321",IsPrimary=true});
    await contacts.SavePhoneAsync(new PatientPhone{PatientId=2,Number="11 12345-6789",IsPrimary=true});
    await contacts.SavePhoneAsync(new PatientPhone{PatientId=1,Number="123"});
    Check((await contacts.GetPhonesAsync(1)).Count==3,"Multiple contacts and incomplete numbers retained");
    Check((await contacts.GetPhonesAsync(1)).Count(x=>x.IsPrimary)==1,"At most one primary phone");
    Check((await contacts.GetPhonesAsync(2)).Single().NormalizedNumber==primary.NormalizedNumber,"Shared telephone allowed across patients");
    await Reject(()=>contacts.SavePhoneAsync(new PatientPhone{PatientId=1,Number="11 12345-6789"}),"Duplicate normalized patient telephone rejected");
    await Reject(()=>contacts.SavePhoneAsync(new PatientPhone{PatientId=4,Number="11 11111-1111"}),"Cross-account telephone rejected");
    await Reject(()=>c.InsertAsync(new FamilyMember{UserId=2,FamilyId=1,PatientId=1,CreatedAt=DateTime.UtcNow}),"Composite foreign keys reject owner mismatch");
    await Reject(()=>patients.UpdateAsync(new Patient{Id=4,Name="Attempt"}),"Patient ORM update cannot steal ownership");
    await Reject(()=>houses.UpdateAsync(new House{CasaId=20,Rua="Attempt"}),"House ORM update cannot steal ownership");
    var houseFirst=await houses.GetByIdAsync(10);var houseStale=await houses.GetByIdAsync(10);
    houseFirst!.Complemento="Updated complement";await houses.UpdateAsync(houseFirst);
    houseStale!.Complemento="Stale overwrite";
    await Reject(()=>houses.UpdateAsync(houseStale),"Stale residence edit cannot overwrite newer address data");
    var p=await patients.GetByIdAsync(1);p!.SusNumbers=["111111111111111","222222222222222"];await patients.UpdateAsync(p);
    Check((await contacts.GetSusNumbersAsync(1)).Count==2,"Future CNS writes synchronize transactionally");
    Check(await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Patient WHERE Id=1 AND BirthDate IS NULL")==1 && await c.ExecuteScalarAsync<string>("SELECT hex(Observacao) FROM Patient WHERE Id=1")==Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes("Long note\0preserved")),"CNS edits preserve unrelated null dates and original text bytes");
    Check((await patients.GetListAsync("222222222222222",0,20,new PatientListFilterDto())).TotalCount==1,"Search finds secondary CNS through projection");
    Check((await patients.GetListAsync("unmatchable-name",0,20,new PatientListFilterDto())).TotalCount==0,"Nonnumeric search does not match every CNS");
    var firstReader=await patients.GetByIdAsync(1);var staleReader=await patients.GetByIdAsync(1);
    firstReader!.Name="Synthetic edited name";await patients.UpdateAsync(firstReader);
    staleReader!.Name="Stale attempted overwrite";
    await Reject(()=>patients.UpdateAsync(staleReader),"Stale patient edit is rejected instead of overwriting newer data");
    Check((await patients.GetByIdAsync(1))!.Name=="Synthetic edited name","Optimistic concurrency preserves the committed edit");
    var previous=(await contacts.GetMembershipHistoryAsync(2)).Single();
    await families.RemovePersonFromFamily(2);
    Check((await contacts.GetMembershipHistoryAsync(2)).Single().LeftAt!=null,"Member exit closes history");
    await families.AddPeopleToFamily([1,2],10,1,1);
    Check((await contacts.GetMembershipHistoryAsync(2)).Count==2 && (await contacts.GetMembershipHistoryAsync(2)).Last().JoinedAt!=null,"Reentry creates separate membership episode");
    var family=(await c.Table<FamilyRecord>().ToListAsync()).Single(f=>f.HouseId==10 && f.UserId==1);
    await families.MoveFamilyAsync(family.Id,11);
    Check((await c.FindAsync<FamilyRecord>(family.Id)).HouseId==11 && (await contacts.GetResidenceHistoryAsync(family.Id)).Count==2,"Residence transfer retains independent family identity and history");
    Check((await contacts.GetMembershipHistoryAsync(1)).Count==1,"Moving house does not invent new family membership");
    Check(await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Patient WHERE Id=2 AND BirthDate IS NULL")==1,"Family changes never overwrite unrelated unknown personal dates");
    var count=await c.Table<FamilyMember>().CountAsync();
    await Reject(()=>families.AddPeopleToFamily([1,4],11,2,1),"Atomic family transfer rejects foreign-account member");
    Check(await c.Table<FamilyMember>().CountAsync()==count,"Failed transfer leaves family history intact");
    await contacts.AssignTerritoryAsync(new TerritorialAssignment{HouseId=11,CensusSector="C1",SectorCode="O1"});
    await contacts.AssignTerritoryAsync(new TerritorialAssignment{HouseId=11,CensusSector="C2",SectorCode="O2"});
    Check(await c.Table<TerritorialAssignment>().CountAsync(x=>x.HouseId==11)==2 && await c.Table<TerritorialAssignment>().CountAsync(x=>x.HouseId==11 && x.ValidTo==null)==1,"Territorial classification has separate operational/census codes and history");
    var visits=new VisitsService(db,context);await visits.PurgeExpiredVisitsAsync(DateTime.Today);
    await visits.GetVisitRecordGroupsAsync(DateTime.Today.AddYears(-3),null);
    Check(await c.Table<Visits>().CountAsync()==2,"Visit views and former purge retain historical events");
    await patients.DeleteAsync(1);
    Check(!(await c.FindAsync<Patient>(1)).IsActive && await c.Table<PatientPregnancy>().CountAsync()==2,"Individual patient deletion archives with clinical history");
    await houses.DeleteAsync(await c.FindAsync<House>(11));
    Check(!(await c.FindAsync<House>(11)).IsActive && await c.Table<Visits>().CountAsync()==2,"Individual residence removal retains historical evidence");
    await c.CloseAsync();c=new SQLiteAsyncConnection(path);db=new DatabaseService(c);await db.InitializeAsync();
    Check(await c.Table<PatientPhone>().CountAsync()==4,"Reopen preserves contacts without duplication");
    Check(await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SchemaMigration")==SchemaMigrator.CurrentVersion,"Restart does not rerun completed backfills");
    var cleanup=new UserDataCleanupService(db,context,new NoReminders());
    await cleanup.DeleteAsync(UserDataDeletionScope.Houses);
    Check((await c.FindAsync<Patient>(4)).HouseId==20 && (await c.FindAsync<House>(20)).IsActive,"Selective residence cleanup preserves other accounts");
    await cleanup.DeleteAsync(UserDataDeletionScope.Patients);
    Check(await c.Table<Patient>().CountAsync(p=>p.UserId==1)==0 && await c.Table<PatientPregnancy>().CountAsync(p=>p.UserId==1)==0,"Explicit patient-data cleanup deletes dependents in safe order");
    Check(await c.Table<Patient>().CountAsync(p=>p.UserId==2)==1 && await c.Table<Patient>().CountAsync(p=>p.UserId==0)==1,"Selective cleanup preserves other users and unowned data");
    await cleanup.DeleteAsync(UserDataDeletionScope.All);
    Check(await c.Table<House>().CountAsync(h=>h.UserId==1)==0 && await c.Table<House>().CountAsync(h=>h.UserId==2)==1,"Full account cleanup preserves other accounts");
    await c.RunInTransactionAsync(SchemaMigrator.ValidateIntegrity);
    Check(true,"SQLite integrity and foreign_key_check pass after migrations and cleanup");
    var restored=DatabaseRecovery.RestoreToNewPath(migrator.BackupPath!,Path.Combine(directory,"restored.db"));
    var restoredConnection=new SQLiteAsyncConnection(restored);await new SchemaMigrator(restoredConnection).RunAsync();
    Check(await restoredConnection.Table<Patient>().CountAsync()==7,"Restored old snapshot migrates with original records");
    await restoredConnection.CloseAsync();await c.CloseAsync();
    var backup=new DatabaseBackupService(db);
    var staged=await backup.StageRestoreForNextStartAsync(migrator.BackupPath!);
    Check(DatabaseRecovery.GetSelectedDatabasePath(directory)==staged && File.Exists(path),"Restoration selects a verified new database for next startup while keeping original file");
    var final=new SQLiteAsyncConnection(staged);await new DatabaseService(final).InitializeAsync();
    Check(await final.Table<Patient>().CountAsync()==7,"Selected restored database opens successfully after restart");
    await final.CloseAsync();
}
async Task ImportIntegration()
{
    var c=new SQLiteAsyncConnection(Path.Combine(directory,"imports.db"));
    var db=new DatabaseService(c);await db.InitializeAsync();
    var context=new CurrentUserContext();context.SetCurrentUser(1);
    var patients=new PatientService(new SQLitePatientRepository(db,context));
    var houses=new HouseService(new SQLiteHouseRepository(db,context));
    var family=new FamilyService(db,context);
    var reader=new RowsReader();
    var history=new ImportHistoryService(db,context);
    var importer=new PatientImportService(patients,new OfflineCep(),new SQLiteConditionsRepository(db,context),new SQLitePatientBolsaFamiliaRepository(db,context),reader,new PatientFamilyLinkResolver(patients,houses,family),history);
    reader.Rows=[
        ["Nome","CNS","Data de nascimento","Sexo","Rua","Numero","Bairro","Cidade","Estado","Responsavel familiar","SUS do responsavel","Diabetes"],
        ["Pessoa Responsavel Sintetica","111111111111111","01/01/1980","F","Rua Sintetica","99","Centro","Cidade","SP","SIM","","SIM"],
        ["Pessoa Dependente Sintetica","222222222222222;333333333333333","01/01/2010","M","Rua Sintetica","99","Centro","Cidade","SP","","111111111111111",""]];
    using var input=new MemoryStream();
    await importer.ImportAsync(input,new PatientImportColumnMapDto{SusNumberColumn="CNS",SourceFileName="synthetic.xlsx"});
    Check(await c.Table<Patient>().CountAsync()==2 && await c.Table<House>().CountAsync()==1,"Patient import uses real SQLite repositories offline");
    Check(await c.Table<FamilyRecord>().CountAsync()==1 && await c.Table<FamilyMember>().CountAsync(x=>x.LeftAt==null)==2,"Imported responsible and dependent synchronize family projection");
    var importedFamily=(await c.Table<FamilyRecord>().ToListAsync()).Single();
    var importedPeople=await c.Table<Patient>().ToListAsync();
    var visits=new VisitsService(db,context);
    var request=new VisitBatchRequestDto{HouseId=importedFamily.HouseId!.Value,FamilyId=importedFamily.LocalNumber!.Value,VisitDate=DateTime.Today,People=importedPeople.Select(p=>new VisitBatchPersonRequestDto{PatientId=p.Id,Status=VisitStatus.Completed}).ToList()};
    Check(await visits.RegisterFamilyVisitBatchAsync(request)==2 && await c.Table<VisitCareLine>().CountAsync()>0,"Visit batch and care-line snapshots commit together");
    await Reject(()=>visits.RegisterFamilyVisitBatchAsync(request),"Duplicate visit batch rejected atomically");
    Check(await c.Table<VisitBatch>().CountAsync()==1 && await c.Table<Visits>().CountAsync()==2,"Failed visit batch leaves no orphan batch or event");
    Check(await c.Table<PatientSusNumber>().CountAsync()==3,"Imported multiple CNS synchronize normalized table");
    Check(await c.ExecuteScalarAsync<int>("SELECT ConditionId FROM PatientConditions WHERE Description='Diabetes'")==2,"Imported condition gets stable catalog identity");
    var conditionRepo=new SQLiteConditionsRepository(db,context);
    var uppercaseCondition=new PatientConditions{PatientId=importedPeople[0].Id,Description="HIPERTENSÃO"};
    await conditionRepo.InsertConditionAsync(uppercaseCondition);
    Check(uppercaseCondition.ConditionId==3 && (await c.FindAsync<PatientConditions>(uppercaseCondition.Id)).Description=="HIPERTENSÃO","Stable clinical identity handles Unicode case while preserving original description");
    await importer.ImportAsync(input,new PatientImportColumnMapDto{SusNumberColumn="CNS"});
    Check(await c.Table<Patient>().CountAsync()==2 && await c.Table<FamilyMember>().CountAsync()==2,"Reimport does not duplicate patients or membership history");
    reader.Rows=[["Rua","Numero","Bairro","Cidade","Estado"],["Rua Sintetica","99","Centro","Cidade","SP"],["Rua Nova","100","Centro","Cidade","SP"]];
    await new HouseImportService(houses,new OfflineCep(),reader,history).ImportAsync(input,new HouseImportColumnMapDto());
    Check(await c.Table<House>().CountAsync()==2,"Residence import merges existing and creates distinct house");
    await c.CloseAsync();c=new SQLiteAsyncConnection(c.DatabasePath);await new DatabaseService(c).InitializeAsync();
    Check(await c.Table<PatientSusNumber>().CountAsync()==3 && await c.Table<ImportHistory>().CountAsync()==3,"Imported projections and reports persist across restart");
    reader.Rows=[["Nome","Nome da mãe","Data de nascimento"],["Pessoa Sem CNS Sintetica","Mae Sintetica","02/02/1990"]];
    await importer.ImportAsync(input,new PatientImportColumnMapDto());
    var withoutSus=(await patients.GetAllPatients())!.Single(p=>p.Name.Equals("Pessoa Sem CNS Sintetica",StringComparison.OrdinalIgnoreCase));
    Check(withoutSus.SusNumbers.Count==0 && await c.Table<PatientSusNumber>().CountAsync(n=>n.PatientId==withoutSus.Id)==0,"Spreadsheet without SUS column creates patient with zero SUS entities");
    reader.Rows=[["Nome","CNS","Nome da mãe","Data de nascimento"],["Pessoa Sem CNS Sintetica","777777777777777; 888.8888.8888.8888;777777777777777","Mae Sintetica","02/02/1990"]];
    await importer.ImportAsync(input,new PatientImportColumnMapDto{SusNumberColumn="CNS"});
    var savedNumbers=await c.Table<PatientSusNumber>().Where(n=>n.PatientId==withoutSus.Id).ToListAsync();
    Check(savedNumbers.Count==2 && savedNumbers.Count(n=>n.IsPrimary)==1,"Semicolon-separated SUS imports create distinct entities with one primary");
    reader.Rows=[["Nome","CNS","Nome da mãe","Data de nascimento"],["Pessoa Sem CNS Sintetica","","Mae Sintetica","02/02/1990"]];
    await importer.ImportAsync(input,new PatientImportColumnMapDto{SusNumberColumn="CNS"});
    Check((await c.Table<PatientSusNumber>().Where(n=>n.PatientId==withoutSus.Id).ToListAsync()).Select(n=>n.Id).Order().SequenceEqual(savedNumbers.Select(n=>n.Id).Order()),"Blank imported SUS preserves existing entities and identities");
    await c.CloseAsync();
}
sealed class RowsReader : ISpreadsheetReader
{
    public List<List<string>> Rows { get; set; }=[];
    public List<List<string>> ReadWorksheetRows(Stream input)=>Rows;
}
sealed class OfflineCep : ICepService
{
    public Task<House?> GetAddressByCepAsync(string cep)=>Task.FromResult<House?>(null);
}
sealed class NoReminders : INoteReminderService
{
    public Task<bool> EnsurePermissionAsync()=>Task.FromResult(true);
    public Task ReconcileAsync()=>Task.CompletedTask;
    public Task ScheduleAsync(int id,DateTime time,string message)=>Task.CompletedTask;
    public Task CancelAsync(int id)=>Task.CompletedTask;
}
