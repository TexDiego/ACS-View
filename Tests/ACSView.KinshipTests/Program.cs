using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.UseCases.Services;
using ACS_View.Infrastructure.Services;
using ACS_View.Infrastructure.Data.Migrations;
using SQLite;

SQLitePCL.Batteries_V2.Init();
int checks=0;
void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
void Reject(Action action,string name){try{action();}catch(InvalidOperationException){checks++;return;}throw new Exception("Expected rejection: "+name);}
var people=Enumerable.Range(1,40).ToDictionary(i=>i,i=>new Patient{Id=i,UserId=1,Name="Pessoa "+i,BirthDate=new DateTime(1950+i,1,1),HouseId=i});
int next=0;
FamilyRelationship Link(int from,int to,string type="parent",string nature="Não informado",bool active=true)=>new(){Id=++next,UserId=1,FromPatientId=from,ToPatientId=to,TypeCode=type,Nature=nature,ForwardLabel=KinshipCatalog.Get(type).Forward,ReverseLabel=KinshipCatalog.Get(type).Reverse,IsActive=active};
KinshipSnapshot S(params FamilyRelationship[] links)=>new(1,people,links);
bool Has(KinshipSnapshot s,int who,int other,string type)=>KinshipEngine.Query(s,who).Any(r=>r.PatientId==other&&r.TypeCode==type);
foreach(var label in new[]{"Pai","Mãe"}){
 var a=Link(1,3,nature:"Biológico");a.ForwardLabel=label;var b=Link(2,3,nature:"Adotivo");b.ForwardLabel=label;
 KinshipEngine.Validate(S(a,b));Check(Has(S(a,b),3,1,"parent")&&Has(S(a,b),1,3,"child"),"two parents reciprocal");
 Reject(()=>KinshipEngine.Validate(S(a,b,Link(4,3))),"third parent");
}
var r1=Link(1,2,nature:"Socioafetivo");var r2=Link(2,3,nature:"Adotivo");var r3=Link(3,4,nature:"Biológico");
var chain=S(r1,r2,r3);KinshipEngine.Validate(chain);
Check(Has(chain,4,1,"greatgrandparent")&&Has(chain,1,4,"greatgrandchild"),"greatgrandparent reciprocity");
Check(Has(chain,3,1,"grandparent")&&Has(chain,1,3,"grandchild"),"grandparent reciprocity");
Check(KinshipEngine.Query(chain,4).Single(r=>r.PatientId==1).Paths.Single().Natures.Contains("Socioafetivo"),"nature in paths");
Reject(()=>KinshipEngine.Validate(S(Link(1,1))),"self");Reject(()=>KinshipEngine.Validate(S(r1,r2,Link(3,1))),"cycle");
Reject(()=>KinshipEngine.Validate(S(Link(1,2),Link(1,2))),"duplicate");
var full=S(Link(1,3),Link(2,3),Link(1,4),Link(2,4));Check(KinshipEngine.Query(full,3).Single(r=>r.PatientId==4&&r.TypeCode=="sibling").SiblingClassification.Contains("completos"),"full siblings");
var half=S(Link(1,3,nature:"Biológico"),Link(2,3,nature:"Biológico"),Link(1,4,nature:"Biológico"),Link(5,4,nature:"Biológico"));Check(KinshipEngine.Query(half,3).Single(r=>r.PatientId==4&&r.TypeCode=="sibling").SiblingClassification.StartsWith("Meio"),"half siblings");
var unknown=S(Link(1,3),Link(1,4));Check(!KinshipEngine.Query(unknown,3).Single(r=>r.PatientId==4).SiblingClassification.StartsWith("Meio"),"unknown second parent");
var uncle=S(Link(1,2),Link(1,3),Link(2,4));Check(Has(uncle,4,3,"uncle")&&Has(uncle,3,4,"nephew"),"uncle/nephew");
var declared=Link(1,3,"grandparent");var both=S(r1,r2,declared);Check(KinshipEngine.Query(both,3).Single(r=>r.PatientId==1&&r.TypeCode=="grandparent") is {IsDerived:true,DeclaredIds.Count:1},"merged provenance");
Check(!Has(S(r1),3,1,"grandparent"),"removed inference");Check(Has(S(declared),3,1,"grandparent"),"independent declared remains");
Check(KinshipEngine.Query(S(declared),3).Single().Paths.Count==0,"no invented intermediate");
var many=Enumerable.Range(1,5).Select(i=>Link(i,20,"grandparent")).ToArray();Reject(()=>KinshipEngine.Validate(S(many)),"four grandparents");
Reject(()=>KinshipEngine.Validate(S(Enumerable.Range(1,9).Select(i=>Link(i,20,"greatgrandparent")).ToArray())),"eight greatgrandparents");
var dedup=S(Link(1,3),Link(1,4),Link(3,5),Link(4,5));Check(KinshipEngine.Query(dedup,5).Count(r=>r.TypeCode=="grandparent"&&r.PatientId==1)==1,"ancestor dedup");
foreach(var code in new[]{"marriage","union","partner"}){
 var spouse=Link(1,2,code);KinshipEngine.Validate(S(spouse));Check(Has(S(spouse),1,2,code)&&Has(S(spouse),2,1,code),"union reciprocal "+code);
 Check(!Has(S(spouse,Link(1,3)),3,2,"parent"),"no inferred parent "+code);
}
var h1=Link(1,2,"marriage",active:false);h1.StartDate=new DateTime(2000,1,1);h1.EndDate=new DateTime(2010,1,1);h1.EndReason="Divórcio";
var h2=Link(1,2,"marriage",active:false);h2.StartDate=new DateTime(2011,1,1);h2.EndDate=new DateTime(2015,1,1);
KinshipEngine.Validate(S(h1,h2,Link(1,3,"partner")));Check(KinshipEngine.Query(S(h1,h2),1).Single().DeclaredIds.Count==2,"historical unions");
foreach(var code in new[]{"godparent","inlaw","step","custom"}){var r=Link(1,2,code);r.ForwardLabel="Tutor";r.ReverseLabel="Tutoreado";Check(KinshipEngine.Query(S(r),1).Single().Label=="Tutoreado"&&KinshipEngine.Query(S(r),2).Single().Label=="Tutor","reciprocal labels "+code);}
KinshipEngine.Validate(S(Link(1,2,"uncle"),Link(1,2,"godparent")));checks++;
var normalized=Link(3,1,"sibling");KinshipEngine.Normalize(normalized);Check(normalized.FromPatientId==1,"symmetric normalization");
var graph=FamilyGraphBuilder.Build(chain,3,new());Check(graph.Nodes.Select(n=>n.PatientId).Distinct().Count()==graph.Nodes.Count,"graph dedup");Check(graph.Nodes.Single(n=>n.PatientId==1).Y<graph.Nodes.Single(n=>n.PatientId==3).Y,"generations");
var filtered=FamilyGraphBuilder.Build(chain,3,new(Nature:"Biológico"));Check(!filtered.Nodes.Any(n=>n.PatientId==2),"nature graph filter");
var customGraph=FamilyGraphBuilder.Build(S(Link(1,2,"godparent")),1,new(Network:true));Check(customGraph.Nodes.Count==2,"network social links");
Check(FamilyGraphBuilder.Build(S(declared),3,new()).Nodes.Count==2,"incomplete graph has no fake nodes");
var preview=KinshipEngine.Preview(S(Link(1,2)),S(Link(1,2),Link(2,3,"sibling")),Link(2,3,"sibling"));Check(preview.Suggestions.Count==1,"optional suggestions");

FamilyGraphLayoutTests.Run(Check);
foreach(var code in new[]{"marriage","union","partner"})Reject(()=>KinshipEngine.Validate(S(Link(1,1,code))),"self spouse "+code);

var dir=Path.Combine(Path.GetTempPath(),"acs-kinship-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
var db=new SQLiteAsyncConnection(Path.Combine(dir,"test.db"));var database=new DatabaseService(db);await database.InitializeAsync();
var context=new CurrentUserContext();context.SetCurrentUser(1);var service=new KinshipService(database,context);
await db.InsertAsync(new User{Id=1,Username="test"});await db.InsertAsync(new User{Id=2,Username="other"});
foreach(var p in people.Values){await db.InsertAsync(new House{CasaId=p.HouseId,UserId=1});await db.InsertAsync(p);}
await db.ExecuteAsync("INSERT INTO Patient(Id,UserId,Name) VALUES(99,2,'Isolated')");
var saved=Link(1,2);saved.Id=0;await service.SaveAsync([saved],[]);
var first=(await service.LoadAsync()).Relationships.Single();Check(first.Id>0,"autoincrement persistence");
await new SchemaMigrator(db).RunAsync();Check((await service.LoadAsync()).Relationships.Count==1,"idempotent migration");
try{var invalid=Link(1,99);invalid.Id=0;await service.SaveAsync([invalid],[]);throw new Exception("profile boundary");}catch(InvalidOperationException){checks++;}
try{var good=Link(2,3);good.Id=0;var cycle=Link(2,1);cycle.Id=0;await service.SaveAsync([good,cycle],[]);throw new Exception("transaction must fail");}catch(InvalidOperationException){checks++;}
Check((await service.LoadAsync()).Relationships.Count==1,"rollback full batch");
var edited=first.Copy();edited.Nature="Adotivo";await service.SaveAsync([edited],[]);
try{await service.SaveAsync([edited],[]);throw new Exception("stale revision");}catch(InvalidOperationException){checks++;}
await service.SaveAsync([],[first.Id]);Check(await db.Table<Patient>().CountAsync()==41,"delete relationship preserves people");
try{await db.ExecuteAsync("INSERT INTO FamilyRelationship(UserId,FromPatientId,ToPatientId,TypeCode,ForwardLabel,ReverseLabel,Nature,IsActive,EndReason,CreatedAt,UpdatedAt) VALUES(1,1,99,'parent','Pai','Filho','Não informado',1,'Não informado',0,0)");throw new Exception("FK");}catch(SQLiteException){checks++;}

var rowCount=await db.Table<Patient>().CountAsync();var newPatient=new Patient{Name="Novo cadastro",HouseId=2};
var pendingParent=Link(1,0,nature:"Adotivo");pendingParent.Id=0;
await service.SaveRegistrationAsync(newPatient,[pendingParent],[]);
Check(newPatient.Id>0 && Has(await service.LoadAsync(),newPatient.Id,1,"parent"),"new patient and pending relationship saved atomically");
var invalidPatient=new Patient{Name="Cadastro inválido",HouseId=3};var badPending=Link(99,0);badPending.Id=0;
try{await service.SaveRegistrationAsync(invalidPatient,[badPending],[]);throw new Exception("rollback new registration");}catch(InvalidOperationException){checks++;}
Check(invalidPatient.Id==0 && await db.Table<Patient>().CountAsync()==rowCount+1,"failed relationship rolls back patient identity and insertion");
var originalPatient=await db.Table<Patient>().FirstAsync(p=>p.Id==3);var originalName=originalPatient.Name;originalPatient.Name="Alteração revertida";
var invalidEdit=Link(99,3);invalidEdit.Id=0;
try{await service.SaveRegistrationAsync(originalPatient,[invalidEdit],[]);throw new Exception("rollback edited patient");}catch(InvalidOperationException){checks++;}
Check((await db.Table<Patient>().FirstAsync(p=>p.Id==3)).Name==originalName,"failed relationship rolls back patient edits");
var legacyChild=new Patient{Name="Filho com pai legado",FatherPatientId=1,HouseId=3};await service.SaveRegistrationAsync(legacyChild,[],[]);
Check(Has(await service.LoadAsync(),legacyChild.Id,1,"parent"),"existing parent picker synchronizes graph");
var mixed=S(Link(1,3),Link(2,3),Link(4,1),Link(5,1),Link(6,2),Link(7,2),Link(8,3,"grandparent"));Reject(()=>KinshipEngine.Validate(mixed),"combined direct and derived grandparent limit");
var directCycle=S(Link(1,3,"grandparent"),Link(3,1));Reject(()=>KinshipEngine.Validate(directCycle),"cycle through explicitly declared missing generation");
context.SetCurrentUser(2);Check((await service.LoadAsync()).People.Keys.SequenceEqual(new[]{99}) && (await service.LoadAsync()).Relationships.Count==0,"queries isolate profile");context.SetCurrentUser(1);
var found=await service.SearchAsync("Pessoa",0,default);Check(found.All(p=>p.UserId==1)&&found.Count<=50,"search scoped and bounded");

var migrationDb=new SQLiteAsyncConnection(Path.Combine(dir,"version5.db"));await new DatabaseService(migrationDb).InitializeAsync();
await migrationDb.InsertAsync(new User{Username="Migration"});await migrationDb.InsertAsync(new Patient{UserId=1,Name="Pai legado"});await migrationDb.InsertAsync(new Patient{UserId=1,Name="Filha legada",FatherPatientId=1});
await migrationDb.RunInTransactionAsync(c=>{
    c.Execute("DROP TABLE FamilyRelationship");c.Execute("DROP TABLE FamilyRelationshipType");
    c.Execute("DROP TABLE PatientInfectiousFollowUp");c.Execute("DROP TABLE PatientStripPickup");c.Execute("DROP TABLE PatientGlucoseMonitoring");
    c.Execute("DELETE FROM SchemaMigration WHERE Version>=6");c.Execute("DELETE FROM SchemaPreservation WHERE MigrationVersion>=6");c.Execute("PRAGMA user_version=5");
});
try{await new SchemaMigrator(migrationDb){BeforeValidation=v=>{if(v==6)throw new InvalidOperationException("injected");}}.RunAsync();throw new Exception("migration rollback");}catch(InvalidOperationException){checks++;}
Check(await migrationDb.ExecuteScalarAsync<int>("PRAGMA user_version")==5 && (await migrationDb.GetTableInfoAsync("FamilyRelationship")).Count==0,"migration failure rolls back new tables and version");
await new SchemaMigrator(migrationDb).RunAsync();
Check(await migrationDb.Table<Patient>().CountAsync()==2 && (await migrationDb.Table<FamilyRelationship>().ToListAsync()).Single().FromPatientId==1,"existing database preserves patients and migrates explicit parent IDs");
await new SchemaMigrator(migrationDb).RunAsync();Check(await migrationDb.Table<FamilyRelationship>().CountAsync()==1,"legacy backfill is idempotent");await migrationDb.CloseAsync();
await db.CloseAsync();Console.WriteLine($"{checks} kinship checks passed.");
