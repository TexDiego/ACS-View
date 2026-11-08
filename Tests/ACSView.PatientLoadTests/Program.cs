using ACS_View.Application.DTOs;
using System.Diagnostics;
using System.Reflection;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using ACS_View.ViewModels;
using SQLite;

SQLitePCL.Batteries_V2.Init();
await HealthModuleTests.Run();
var checks = 0;
void Check(bool pass, string message)
{
    if (!pass) throw new InvalidOperationException(message);
    checks++;
    Console.WriteLine("PASS: " + message);
}

// No database service may be called while initializing an empty form.
var empty = new AddRegisterViewModel(Reject<IPatientService>(), Reject<ICidRepository>(),
    Reject<IPatientCidRepository>(), Reject<ISQLiteConditionsRepository>(), Reject<IPatientBolsaFamiliaRepository>(),
    Reject<IPatientInsulinDependencyRepository>(), Reject<IPregnancyService>(), Reject<ICareNotificationService>(),
    Reject<IPopupService>(), Reject<IPatientPhoneService>());
empty.InitializeNewPatient();
Check(empty.CommonConditions.Count > 0 && empty.CurrentPatient.Id == 0, "New form initializes common conditions with zero service calls");
Check(empty.PhoneEditor.Phones.Count == 0 && empty.SusEditor.Numbers.Count == 0, "New form has empty phone and SUS editors");
Check(BaseViewModel.Alerts.Count == 0, "No initialization errors");

var directory = Path.Combine(Path.GetTempPath(), "acs-patient-load-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
Console.WriteLine("Synthetic fixture: " + directory);
var db = new SQLiteAsyncConnection(Path.Combine(directory, "benchmark.db"));
var database = new DatabaseService(db);
await database.InitializeAsync();
var user = new CurrentUserContext();
user.SetCurrentUser(1);
await db.RunInTransactionAsync(c =>
{
    c.Insert(new User { Id = 1, Username = "Benchmark" });
    c.Insert(new User { Id = 2, Username = "Other account" });
    for (var i = 1; i <= 10000; i++)
    {
        c.Insert(new Patient { Id = i, UserId = 1, Name = "Synthetic " + i, BirthDate = new DateTime(1990, 1, 1), MotherPatientId = i is 2 or 3 ? 1 : null });
        c.Insert(new PatientSusNumber { UserId = 1, PatientId = i, Number = (700000000000000L + i).ToString(), IsPrimary = true });
    }
    c.Insert(new Patient { Id = 10001, UserId = 2, Name = "Other mother" });
    c.Insert(new Patient { Id = 10002, UserId = 2, Name = "Other child", MotherPatientId = 10001 });
    for (var i = 1; i <= 15000; i++)
        c.Insert(new CidSubcategory { Id = i, Code = "C" + i.ToString("D5"), Description = "Synthetic CID " + i, CategoryCode = "C" });
    c.Insert(new PatientCID { UserId = 1, PatientId = 1, CidId = 14999 });
    c.Insert(new PatientPhone { UserId = 1, PatientId = 1, Number = "11912345678", IsPrimary = true });
    c.Insert(new PatientBolsaFamilia { UserId = 1, PatientId = 1, ResponsiblePatientId = 2 });
    c.Insert(new PatientInsulinDependency { UserId = 1, PatientId = 1 });
    c.Insert(new PatientConditions { UserId = 1, PatientId = 1, Description = HealthConditionCatalog.Diabetes });
    c.Insert(new PatientPregnancy { UserId = 1, PatientId = 1, Status = PregnancyStatus.Active, LastMenstrualPeriod = DateTime.Today.AddDays(-100), CreatedAt = DateTime.UtcNow });
});
var patients = new PatientService(new SQLitePatientRepository(database, user));
var cids = new SQLiteCidRepository(db);
var patientCids = new PatientCidService(database, user);
var conditions = new SQLiteConditionsRepository(database, user);
var bolsa = new SQLitePatientBolsaFamiliaRepository(database, user);
var insulin = new SQLitePatientInsulinDependencyRepository(database, user);
var pregnancy = new PregnancyService(database, user);
var phones = new PatientPhoneService(new SQLitePatientPhoneRepository(database, user, new RelationalDataService(database, user)));
AddRegisterViewModel CreateEditor() => new(patients, cids, patientCids, conditions, bolsa, insulin, pregnancy,
    Noop<ICareNotificationService>(), Reject<IPopupService>(), phones, glucoseMonitoring: new GlucoseMonitoringService(database, user), infectiousFollowUp: new PatientInfectiousFollowUpService(database,user));

// An existing mother can open a first pregnancy before an episode is saved.
await db.InsertAsync(new Patient { UserId = 1, Name = "Child for first episode", MotherPatientId = 2 });
var firstEpisodeEditor = CreateEditor();
await firstEpisodeEditor.LoadPatient(2);
var draftDetails = await (Task<PregnancyDetailsDto>)typeof(AddRegisterViewModel)
    .GetMethod("BuildDraftPregnancyDetailsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
    .Invoke(firstEpisodeEditor, new object[] { 2 })!;
Check(draftDetails.RegisteredChildrenCount == 1, "First pregnancy draft counts existing MotherPatientId links");

await Bench("Old opening: full CID catalog", async () => { await cids.GetAllSubcategories(); });
await Bench("New form: local initialization", () => { CreateEditor().InitializeNewPatient(); return Task.CompletedTask; });
await Bench("Patient and SUS (targeted)", async () => { await patients.GetPatientById(1); });
await Bench("Old SUS hydration: entire account", async () => { await db.Table<PatientSusNumber>().Where(n => n.UserId == 1).ToListAsync(); });
await Bench("Phones", async () => { await phones.GetPhonesByPatientId(1); });
await Bench("Bolsa Familia and responsible", async () => { var b = await bolsa.GetByPatientIdAsync(1); await patients.GetPatientById(b!.ResponsiblePatientId); });
await Bench("Insulin dependency", async () => { await insulin.GetByPatientIdAsync(1); });
await Bench("Pregnancy details", async () => { await pregnancy.GetDetailsByPatientIdAsync(1); });
await Bench("Old child count: all patients", async () => { PregnancyCalculator.CalculateRegisteredChildrenCount(1, (await db.Table<Patient>().Where(p => p.UserId == 1).ToListAsync())); });
await Bench("Conditions", async () => { await conditions.GetConditionsByPatientIdAsync(1); });
await Bench("Patient CID links", async () => { await patientCids.GetPatientCIDsByPatientId(1); });
await Bench("Linked CID only", async () => { await cids.GetSubcategoryById(14999); });
await Bench("Edit form: complete loading", async () => { await CreateEditor().LoadPatient(1); });
await Bench("CID selector: search 5 results", async () => { await cids.SearchCidAsync("Synthetic", null, 0, 5); });

await db.InsertAsync(new PatientConditions { UserId=1, PatientId=1, Description=HealthConditionCatalog.Imunodeficiente });
var editor = CreateEditor();
await editor.LoadPatient(1);
Check(editor.CommonConditions.Single(c=>c.IsImmuneGroup).ImmunodeficiencySelected && !editor.CommonConditions.Single(c=>c.IsImmuneGroup).HivSelected, "Registration: legacy immunodeficiency does not preselect HIV");
Check(BaseViewModel.Alerts.Count == 0 && !editor.IsLoading, "Editing completes without errors");
Check(editor.SusEditor.Numbers.Count == 1 && editor.PhoneEditor.Phones.Count == 1, "Editing loads only this patient's SUS and phones");
Check(editor.HealthCategories.Single().CID == "C14999", "Linked CID restored without loading the catalog");
Check(editor.IsBolsaFamiliaSelected && editor.HasBolsaFamiliaResponsible && editor.BolsaFamiliaResponsibleNameInput == "Synthetic 2", "Benefit and responsible restored");
Check(editor.CommonConditions.Single(c => c.IsDiabetesCondition).Selected && editor.IsInsulinDependent, "Conditions and insulin dependency restored");
Check(editor.IsPregnancySelected && (await pregnancy.GetDetailsByPatientIdAsync(1))!.RegisteredChildrenCount == 2, "Pregnancy counts children only from the current account");
user.SetCurrentUser(2);
Check(await patients.GetPatientById(1) is null && await pregnancy.GetDetailsByPatientIdAsync(1) is null, "Patient and pregnancy remain isolated between accounts");
user.SetCurrentUser(1);

var selector = new ConditionPopupViewModel(cids);
await selector.LoadPatientCid(editor.HealthCategories.ToList());
selector.SwitchSelected.Execute(selector.HealthCategories.Single());
Check(editor.HealthCategories.Single().Selected, "Dismissing selector cannot change the original selection");
selector.SearchText = "Synthetic";
await WaitUntil(() => selector.SearchedConditions.Count == 5);
var choice = selector.SearchedConditions.First();
selector.SwitchSelected.Execute(choice);
selector.SearchText = "Synthetic CID 15000";
await WaitUntil(() => selector.SearchedConditions.Count == 1 && selector.SearchedConditions[0].CID == "C15000");
Check(selector.HealthCategories.Contains(choice), "Selected CID survives a different search");
selector.SearchText = "Synthetic";
await WaitUntil(() => selector.SearchedConditions.Count == 5);
Check(selector.SearchedConditions.First().Selected, "Selection remains checked when returning to the search");
selector.SwitchSelected.Execute(selector.SearchedConditions.First());
Check(selector.HealthCategories.Count == 0, "Deselecting removes CID from the result");
selector.SearchText = "Synthetic CID 14999";
selector.SearchText = string.Empty;
await Task.Delay(400);
Check(selector.SearchedConditions.Count == 0, "Cleared search discards pending results");

var newEditor = CreateEditor();
newEditor.InitializeNewPatient();
newEditor.CurrentPatient.Name = "Cadastro de teste";
var immuneGroup = newEditor.CommonConditions.Single(c=>c.IsImmuneGroup);
immuneGroup.Selected=true;immuneGroup.HivSelected=true;
newEditor.CommonConditions.Single(c=>c.ConditionKey==HealthConditionCatalog.Tuberculose).Selected=true;
newEditor.TuberculosisEditor.Status="Em tratamento";newEditor.TuberculosisEditor.Notes="Synthetic TB follow-up";
newEditor.HivEditor.Status="Em acompanhamento";newEditor.HivEditor.ReferenceService="Synthetic HIV service";
newEditor.GlucoseEditor.ChecksPerDay = "4";
newEditor.GlucoseEditor.PickupInterval = "Mensal";
newEditor.GlucoseEditor.StripsPerPickup = "100";
newEditor.HealthCategories.Add(new HealthConditions { Name = "Synthetic CID 15000", CID = "C15000", Selected = true });
await (Task)typeof(AddRegisterViewModel).GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(newEditor, null)!;
Check(BaseViewModel.Alerts.Count == 0 && BaseViewModel.Navigations == 1, "New patient saves successfully without a preloaded CID catalog");
Check((await patientCids.GetPatientCIDsByPatientId(newEditor.CurrentPatient.Id))!.Single().CidId == 15000, "Saving resolves the selected CID by code");
var reopened = CreateEditor();
await reopened.LoadPatient(newEditor.CurrentPatient.Id);
Check(reopened.GlucoseEditor.ChecksPerDay == "4" && reopened.GlucoseEditor.StripsPerPickup == "100", "Registration saves and reloads optional monitoring with patient identity");
reopened.GlucoseEditor.ChecksPerDay = "3";
await new GlucoseMonitoringService(database, user).AddPickupAsync(reopened.CurrentPatient.Id, DateTime.Today, 60, "Known history");
var reopenedGroup=reopened.CommonConditions.Single(c=>c.IsImmuneGroup);
Check(reopenedGroup.Selected && reopenedGroup.HivSelected && !reopenedGroup.ImmunodeficiencySelected
    && reopened.TuberculosisEditor.Notes=="Synthetic TB follow-up" && reopened.HivEditor.ReferenceService=="Synthetic HIV service",
    "Registration: HIV suboption and both follow-ups survive save and reopening without inferring immunodeficiency");
Check(!(await conditions.GetConditionsByPatientIdAsync(reopened.CurrentPatient.Id)).Any(c=>c.Description==HealthConditionCatalog.Imunodeficiente),"Registration: HIV-only selection persists no immunodeficiency diagnosis");
reopened.CurrentPatient.Name = "Cadastro atualizado";
await (Task)typeof(AddRegisterViewModel).GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(reopened, null)!;
Check(BaseViewModel.Alerts.Count == 0 && BaseViewModel.Navigations == 2, "Existing patient saves after targeted loading");
Check((await patients.GetPatientById(reopened.CurrentPatient.Id))!.Name == "Cadastro Atualizado"
    && (await patientCids.GetPatientCIDsByPatientId(reopened.CurrentPatient.Id))!.Single().CidId == 15000,
    "Closing, reopening and saving preserves the linked CID");

var savedGlucose = new GlucoseMonitoringService(database, user);
Check((await savedGlucose.GetAsync(reopened.CurrentPatient.Id))?.ChecksPerDay == 3 && (await savedGlucose.GetPickupsAsync(reopened.CurrentPatient.Id)).Single().Quantity == 60,
    "Registration updates monitoring without replacing pickup history");
Check(await db.ExecuteScalarAsync<string>("PRAGMA integrity_check") == "ok", "Database integrity preserved");

var kinshipService=new KinshipService(database,user);
var kinshipEditor=new KinshipEditorViewModel(kinshipService);
var unsaved=new Patient{Name="Novo familiar"};await kinshipEditor.LoadAsync(unsaved);
await kinshipEditor.StageAsync(new FamilyRelationship{FromPatientId=1,ToPatientId=0,TypeCode="parent",ForwardLabel="Mãe",ReverseLabel="Filha",Nature="Adotivo"});
Check(unsaved.Id==0 && await db.Table<FamilyRelationship>().CountAsync()==0,"Kinship form keeps new relationships pending until patient save");
Check(kinshipEditor.Direct.Count==1 && kinshipEditor.Direct[0].Title.Contains("Mãe"),"Kinship form displays declared roles");
await kinshipEditor.CommitAsync(unsaved);
Check(unsaved.Id>0 && (await kinshipService.LoadAsync()).Relationships.Count==1,"Kinship form commits patient and relationships together");
await kinshipEditor.LoadAsync(unsaved);
await kinshipEditor.StageAsync(new FamilyRelationship{FromPatientId=2,ToPatientId=unsaved.Id,TypeCode="godparent",ForwardLabel="Padrinho",ReverseLabel="Afilhada"});
Check(await db.Table<FamilyRelationship>().CountAsync()==1 && kinshipEditor.Direct.Count==2,"Kinship form keeps edits pending and allows distinct semantic links");
await kinshipEditor.CommitAsync(unsaved);
var kinshipReadOnly=new KinshipEditorViewModel(kinshipService,true);await kinshipReadOnly.LoadAsync(unsaved);
Check(!kinshipReadOnly.CanEdit && kinshipReadOnly.Direct.Count==2 && kinshipReadOnly.Summary.Contains("conhecidos"),"Popup family summary uses persisted scoped graph and explains counts");

await kinshipEditor.LoadAsync(unsaved);var previousLinks=kinshipEditor.Direct.Count;
try{await kinshipEditor.StageAsync(new FamilyRelationship{FromPatientId=unsaved.Id,ToPatientId=unsaved.Id,TypeCode="marriage"});throw new Exception("Self spouse accepted");}
catch(InvalidOperationException){Check(kinshipEditor.Direct.Count==previousLinks,"Self-spouse is rejected without changing the pending form");}
kinshipEditor.OpenEditorRequested=_=>throw new InvalidOperationException("Synthetic navigation failure");
await kinshipEditor.AddCommand.ExecuteAsync(null);
Check(kinshipEditor.Error.Contains("Synthetic navigation failure"),"Failure opening relationship editor becomes a visible error instead of an unhandled command");
await kinshipEditor.StageAsync(new FamilyRelationship{FromPatientId=3,ToPatientId=unsaved.Id,TypeCode="marriage",ForwardLabel="Casamento",ReverseLabel="Casamento"});
Check(kinshipEditor.Direct.Count==previousLinks+1,"A valid spouse can still be added after an invalid attempt");
await db.CloseAsync();
Console.WriteLine($"{checks} patient loading checks passed.");

async Task Bench(string name, Func<Task> action)
{
    var timer = Stopwatch.StartNew();
    await action();
    var first = timer.Elapsed.TotalMilliseconds;
    var samples = new List<double>();
    for (var i = 0; i < 15; i++)
    {
        timer.Restart(); await action(); samples.Add(timer.Elapsed.TotalMilliseconds);
    }
    samples.Sort();
    Console.WriteLine($"TIME: {name}: first={first:F2} ms median={samples[7]:F2} ms p95={samples[14]:F2} ms");
}
static async Task WaitUntil(Func<bool> predicate)
{
    var timer = Stopwatch.StartNew();
    while (!predicate())
    {
        if (timer.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("Selector search did not complete");
        await Task.Delay(20);
    }
}
static T Reject<T>() where T : class => DispatchProxy.Create<T, RejectProxy>();
static T Noop<T>() where T : class => DispatchProxy.Create<T, NoopProxy>();
public class RejectProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new InvalidOperationException("Unexpected service call: " + method!.Name);
}
public class NoopProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Task.CompletedTask;
}
