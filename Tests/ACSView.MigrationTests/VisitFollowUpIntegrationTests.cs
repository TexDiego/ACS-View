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
            var lastCompliantDay = new DateTime(2027, 3, 10);
            var beforeExpiry = (await service.GetVisitSuggestionsAsync(lastCompliantDay)).Single(s => s.PatientId == patient.Id);
            var afterExpiry = (await service.GetVisitSuggestionsAsync(lastCompliantDay.AddDays(1))).Single(s => s.PatientId == patient.Id);
            var hypertension = afterExpiry.Summary!.Evaluations.Single(e => e.CareLine == VisitCareLineType.Hypertension);
            check(beforeExpiry.DueDate == lastCompliantDay && hypertension.Completed == 1 &&
                hypertension.RecommendedDate == lastCompliantDay.AddDays(1) &&
                hypertension.Status == VisitFollowUpStatus.Recommended && !afterExpiry.IsOverdue,
                "Expired hypertension window generates a new suggestion and preserves valid visit evidence");
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
            var overdueChild = new Patient { Name = "Synthetic overdue infant visit", BirthDate = today.AddDays(-31) };
            var dueTodayChild = new Patient { Name = "Synthetic infant visit due today", BirthDate = today.AddDays(-30) };
            await repo.InsertAsync(overdueChild);
            await repo.InsertAsync(dueTodayChild);
            var suggestions = await service.GetVisitSuggestionsAsync(today);
            var overdue = suggestions.Single(s => s.PatientId == overdueChild.Id);
            var dueToday = suggestions.Single(s => s.PatientId == dueTodayChild.Id);
            var prenatal = suggestions.Single(s => s.PatientId == woman.Id);
            check(overdue.IsOverdue && dueToday.DueDate == today && !dueToday.IsOverdue &&
                suggestions.IndexOf(dueToday) < suggestions.IndexOf(overdue) &&
                suggestions.IndexOf(prenatal) < suggestions.IndexOf(overdue),
                "Visits due today and open prenatal recommendations precede expired infant visits");
            await c.InsertAsync(new PatientConditions { UserId = 1, PatientId = overdueChild.Id, Description = "Diabetes" });
            var mixed = (await service.GetVisitSuggestionsAsync(today)).Single(s => s.PatientId == overdueChild.Id);
            check(mixed.Summary?.Primary?.CareLine == VisitCareLineType.Diabetes && !mixed.IsOverdue &&
                mixed.Summary.Evaluations.Any(e => e.CareLine == VisitCareLineType.Child && e.Status == VisitFollowUpStatus.Overdue),
                "An expired infant visit does not hide a current recommendation for the same patient");
            var routinePatient = new Patient { Name = "Synthetic routine adult", BirthDate = today.AddYears(-35) };
            await repo.InsertAsync(routinePatient);
            var hypertensivePatient = new Patient { Name = "Synthetic hypertension priority", BirthDate = today.AddYears(-35) };
            await repo.InsertAsync(hypertensivePatient);
            await c.InsertAsync(new PatientConditions { UserId = 1, PatientId = hypertensivePatient.Id, Description = "Hipertensão" });
            var expiredVisit = new Visits { UserId = 1, PatientId = hypertensivePatient.Id, VisitDate = today.AddYears(-1).AddDays(-1), Status = "Completed" };
            await c.InsertAsync(expiredVisit);
            await c.InsertAsync(new VisitCareLine { UserId = 1, VisitId = expiredVisit.Id, CareLineType = "Hypertension" });
            foreach (var reference in new[] { today, new DateTime(2026, 11, 25), new DateTime(2027, 4, 21) })
            {
                var ordered = await service.GetVisitSuggestionsAsync(reference);
                var routineIndex = ordered.FindIndex(s => s.PatientId == routinePatient.Id);
                var riskIndex = ordered.FindIndex(s => s.PatientId == hypertensivePatient.Id);
                check(riskIndex >= 0 && routineIndex >= 0 && riskIndex < routineIndex,
                    "Continuous risk precedes routine visits before month end and after annual evidence expires");
            }
            var recentlyVisited = new Patient { Name = "AAA recently visited", BirthDate = today.AddYears(-70) };
            var awaitingVisit = new Patient { Name = "ZZZ awaiting visit", BirthDate = today.AddYears(-70) };
            foreach (var person in new[] { recentlyVisited, awaitingVisit })
            {
                await repo.InsertAsync(person);
                foreach (var description in new[] { "Hipertensão", "Diabetes" })
                    await c.InsertAsync(new PatientConditions { UserId = 1, PatientId = person.Id, Description = description });
            }
            await service.RegisterVisitAsync(new Visits { PatientId = recentlyVisited.Id, VisitDate = today.AddDays(-1), Status = "Completed" });
            foreach (var reference in new[] { today, today.AddDays(28) })
            {
                var ordered = await service.GetVisitSuggestionsAsync(reference);
                var recent = ordered.Single(s => s.PatientId == recentlyVisited.Id);
                check(recent.Summary!.Evaluations.All(e => e.Completed == 1 && e.RecommendedDate == today.AddDays(29)) &&
                    ordered.FindIndex(s => s.PatientId == awaitingVisit.Id) < ordered.IndexOf(recent),
                    "Equivalent risk awaiting a visit precedes a patient waiting for the minimum interval");
            }
            var eligibleAgain = await service.GetVisitSuggestionsAsync(today.AddDays(29));
            check(eligibleAgain.FindIndex(s => s.PatientId == recentlyVisited.Id) <
                eligibleAgain.FindIndex(s => s.PatientId == awaitingVisit.Id),
                "On the next eligible date the patient returns to normal risk ordering");
            await new DatabaseService(c).InitializeAsync();
            check((await service.GetVisitRecordGroupsAsync(today,null)).SelectMany(g=>g.Visits).Count()==4,"Reinitialization preserves monthly history");
        }
        finally { await c.CloseAsync(); }
    }
}
