using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

internal static class QuickVisitTests
{
    public static async Task Run(string directory, Action<bool,string> check)
    {
        var c = new SQLiteAsyncConnection(Path.Combine(directory, "quick-visits.db"));
        try
        {
            var db = new DatabaseService(c); await db.InitializeAsync();
            var context = new CurrentUserContext(); context.SetCurrentUser(1);
            var service = new VisitsService(db, context);
            var house = new House { UserId=1, Rua="Synthetic" }; await c.InsertAsync(house);
            var person = new Patient { UserId=1, Name="Maria", HouseId=house.CasaId, BirthDate=new DateTime(1950,1,1) };
            var sibling = new Patient { UserId=1, Name="José", HouseId=house.CasaId };
            await c.InsertAsync(person); await c.InsertAsync(sibling);
            Task<bool> Yes(string t,string m,string a) => Task.FromResult(true);
            Task<bool> No(string t,string m,string a) => Task.FromResult(false);
            check(!await service.RegisterQuickVisitAsync(person.Id, No) && await c.Table<Visits>().CountAsync()==0, "Quick: initial cancel writes nothing");
            check(await service.RegisterQuickVisitAsync(person.Id, Yes), "Quick: first visit succeeds");
            var first=(await service.GetAllVisitsAsync()).Single();
            check(first.PatientId==person.Id && first.BatchId==null && first.Status=="Completed" && first.VisitDate.Date==DateTime.Today && first.Date==first.VisitDate, "Quick: local timestamp and individual completed record");
            check(!(await service.GetPatientVisitSummaryAsync(sibling.Id,DateTime.Today)).LastCompletedVisit.HasValue, "Quick: other resident unchanged");
            var summary=await service.GetPatientVisitSummaryAsync(person.Id,DateTime.Today);
            check(summary.LastCompletedVisit==first.VisitDate && summary.Primary?.RecommendedDate==DateTime.Today.AddDays(30) && summary.MonthlyCompletedVisits==1, "Quick: last visit and next suggestion recalculated");
            var prompts=0;
            check(!await service.RegisterQuickVisitAsync(person.Id,(t,m,a)=>Task.FromResult(++prompts==1)) && prompts==2 && await c.Table<Visits>().CountAsync()==1, "Quick: duplicate cancellation preserves history");
            prompts=0;
            check(await service.RegisterQuickVisitAsync(person.Id,(t,m,a)=>{prompts++;return Task.FromResult(true);}) && prompts==2, "Quick: explicit second visit accepted");
            var recordGroups = await service.GetVisitRecordGroupsAsync(DateTime.Today, VisitStatus.Completed);
            check((await service.GetMonthlyOverviewAsync(DateTime.Today)).CompletedVisits==2 && recordGroups.SelectMany(g=>g.Visits).Count()==2, "Quick: monthly totals and manageable history include both visits");
            check(recordGroups.Single().FamilyName == "Sem família vinculada", "Quick: visits without a family show an explicit label in history");
            check((await service.GetPatientVisitSummaryAsync(person.Id,DateTime.Today)).Primary?.Completed==1, "Quick: same-day repeat does not bypass indicator spacing");
            check((await service.GetAllVisitsAsync()).Any(v=>v.Id==first.Id), "Quick: existing history preserved");
            var entered=new TaskCompletionSource(); var release=new TaskCompletionSource<bool>();
            var pending=service.RegisterQuickVisitAsync(person.Id,(t,m,a)=>{entered.TrySetResult();return release.Task;});
            await entered.Task;
            check(!await service.RegisterQuickVisitAsync(person.Id,Yes), "Quick: concurrent clicks ignored while confirmation open");
            release.SetResult(false); await pending;
            context.SetCurrentUser(2);
            var rejected=false;try { await service.RegisterQuickVisitAsync(person.Id,Yes); } catch(InvalidOperationException) { rejected=true; }
            check(rejected && (await service.GetAllVisitsAsync()).Count==0, "Quick: foreign profile rejected");
            context.SetCurrentUser(1);
            rejected=false;try { await service.RegisterQuickVisitAsync(person.Id,(t,m,a)=>{context.SetCurrentUser(2);return Task.FromResult(true);}); } catch(InvalidOperationException) { rejected=true; }
            check(rejected, "Quick: profile switch during confirmation rejected"); context.SetCurrentUser(1);
            await c.ExecuteAsync("CREATE TRIGGER fail_quick_line BEFORE INSERT ON VisitCareLine BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END");
            var version=DataChangeTracker.VisitsVersion;
            rejected=false;try { await service.RegisterQuickVisitAsync(person.Id,Yes); } catch(SQLiteException) { rejected=true; }
            check(rejected && await c.Table<Visits>().CountAsync()==2 && DataChangeTracker.VisitsVersion==version, "Quick: persistence failure rolls back visit and publishes no success");
            await c.ExecuteAsync("DROP TRIGGER fail_quick_line");
            check(await service.RegisterQuickVisitAsync(person.Id,Yes), "Quick: retry succeeds after persistence failure");
        }
        finally { await c.CloseAsync(); }
    }
}
