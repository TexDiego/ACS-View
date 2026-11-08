using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Services;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.UseCases.Services;
using SQLite;

internal static class VisitFollowUpIntegrationTests
{
    public static async Task Run(string directory, Action<bool,string> check)
    {
        var c = new SQLiteAsyncConnection(Path.Combine(directory,"visit-follow-up.db"));
        try
        {
            var db = new DatabaseService(c); await db.InitializeAsync();
            var context = new CurrentUserContext(); context.SetCurrentUser(1);
            var repo = new SQLitePatientRepository(db,context);
            var patient = new Patient { Name="Synthetic multiple indicators", BirthDate=new DateTime(1950,1,1), SusNumber="111111111111111" };
            await repo.InsertAsync(patient);
            var service = new VisitsService(db,context);
            var today = new DateTime(2026,11,5);
            foreach(var description in new[]{"Diabetes","Hipertensão"})
                await c.InsertAsync(new PatientConditions{UserId=1,PatientId=patient.Id,Description=description});
            var initial = await service.GetPatientVisitSummaryAsync(patient.Id,today);
            check(initial.Evaluations.Count==3 && initial.Evaluations.All(e=>e.Completed==0),"No history never proves compliance for multiple indicators");
            await service.RegisterVisitAsync(new Visits {PatientId=patient.Id,VisitDate=new DateTime(2026,3,10),Status="Completed"});
            await service.RegisterVisitAsync(new Visits {PatientId=patient.Id,VisitDate=new DateTime(2026,4,20),Status="Completed"});
            var summary=await service.GetPatientVisitSummaryAsync(patient.Id,today);
            check(summary.Evaluations.All(e=>e.Completed==2),"Two physical visits satisfy three evidenced care lines");
            check(!(await service.GetVisitSuggestionsAsync(today)).Any(s=>s.PatientId==patient.Id),"Compliant patient not repeatedly suggested each month");
            await service.RegisterVisitAsync(new Visits {PatientId=patient.Id,VisitDate=today.AddDays(-1),Status="Absent"});
            summary=await service.GetPatientVisitSummaryAsync(patient.Id,today);
            check(summary.LastCompletedVisit==new DateTime(2026,4,20) && summary.LastAttemptStatus=="Absent" && summary.Evaluations.All(e=>e.Completed==2),"Absent attempt separate from completed evidence");
            await service.RegisterVisitAsync(new Visits {PatientId=patient.Id,VisitDate=today,Status="Refused"});
            var monthly=await service.GetMonthlyOverviewAsync(today);
            check(monthly.TotalVisits==2 && monthly.CompletedVisits==0 && monthly.AbsentVisits==1 && monthly.RefusedVisits==1 && monthly.DiabetesVisits==2,"Monthly care lines preserve attempted-event semantics");
            check((await service.GetVisitRecordGroupsAsync(today,VisitStatus.Refused)).SelectMany(g=>g.Visits).Count()==1,"Monthly status filter retained");
            await service.RegisterVisitAsync(new Visits {PatientId=patient.Id,VisitDate=today.AddDays(1),Status="Completed"});
            check((await service.GetPatientVisitSummaryAsync(patient.Id,today)).LastAttemptStatus=="Refused","Future visit excluded from last attempt");
            var march=(await service.GetAllVisitsAsync()).Single(v=>v.VisitDate==new DateTime(2026,3,10));
            await service.DeleteVisitAsync(march.Id);
            summary=await service.GetPatientVisitSummaryAsync(patient.Id,today);
            check(summary.Evaluations.All(e=>e.Completed==1),"Deletion recomputes each care-line result");
            check((await service.GetVisitSuggestionsAsync(today)).Count(s=>s.PatientId==patient.Id)==1,"Multiple indicators generate one physical suggestion");
            // Legacy completed event retained, never assigned retroactively to current conditions.
            var legacy=new Visits{UserId=1,PatientId=patient.Id,VisitDate=today.AddDays(-50),Status="Completed"}; await c.InsertAsync(legacy);
            summary=await service.GetPatientVisitSummaryAsync(patient.Id,today);
            check(summary.Evaluations.All(e=>e.Completed==1 && e.IncompleteHistory),"Legacy event without snapshot remains incomplete evidence");
            await service.PurgeExpiredVisitsAsync(today.AddYears(10));
            check(await c.Table<Visits>().CountAsync()==5,"Compatibility purge never deletes old evidence");
            context.SetCurrentUser(2);
            check((await service.GetPatientVisitSummaryAsync(patient.Id,today)).LastCompletedVisit==null && (await service.GetVisitSuggestionsAsync(today)).Count==0,"Patient and collective queries isolate authenticated account");
            context.SetCurrentUser(1);
            check((await service.GetPatientVisitSummaryAsync(patient.Id,new DateTime(2027,4,21))).Evaluations.All(e=>e.Completed==1),"Date advance recalculates without cache or edit");
            // New pregnancy episode must exclude a previous episode even with the same historical line.
            var woman=new Patient{Name="Synthetic pregnancy",SusNumber="222222222222222",BirthDate=new DateTime(1995,1,1)};await repo.InsertAsync(woman);
            await c.InsertAsync(new PatientPregnancy{UserId=1,PatientId=woman.Id,LastMenstrualPeriod=today.AddDays(-500),Status=PregnancyStatus.Ended,EndedAt=today.AddDays(-220),EndType=PregnancyEndType.Birth,CreatedAt=today.AddDays(-500)});
            await c.InsertAsync(new PatientPregnancy{UserId=1,PatientId=woman.Id,LastMenstrualPeriod=today.AddDays(-100),Status=PregnancyStatus.Active,CreatedAt=today.AddDays(-90)});
            var oldVisit=new Visits{UserId=1,PatientId=woman.Id,VisitDate=today.AddDays(-300),Status="Completed"};await c.InsertAsync(oldVisit);
            await c.InsertAsync(new VisitCareLine{UserId=1,VisitId=oldVisit.Id,CareLineType="Pregnancy"});
            check((await service.GetPatientVisitSummaryAsync(woman.Id,today)).Evaluations.Single().Completed==0,"Prior pregnancy evidence never satisfies new episode");
            var child=new Patient{Name="Synthetic age boundary",SusNumber="333333333333333",BirthDate=today.AddYears(-2)};await repo.InsertAsync(child);
            check((await service.GetPatientVisitSummaryAsync(child.Id,today)).Evaluations.All(e=>e.CareLine!=VisitCareLineType.Child),"Child exits eligible population on second birthday");
            await c.ExecuteAsync("UPDATE Patient SET BirthDate=NULL WHERE Id=?",child.Id);
            check((await service.GetPatientVisitSummaryAsync(child.Id,today)).Evaluations.All(e=>e.CareLine!=VisitCareLineType.Child),"Null birth does not materialize as newborn");
            await new DatabaseService(c).InitializeAsync();
            check((await service.GetVisitRecordGroupsAsync(today,null)).SelectMany(g=>g.Visits).Count()==3,"Reinitialization preserves monthly history");
        }
        finally { await c.CloseAsync(); }
    }
}
