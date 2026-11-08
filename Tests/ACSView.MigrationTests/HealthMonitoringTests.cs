using ACS_View.Application.Health;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

internal static class HealthMonitoringTests
{
    public static async Task Run(string directory, Action<bool,string> check)
    {
        var c = new SQLiteAsyncConnection(Path.Combine(directory, "health.db"));
        var db = new DatabaseService(c); await db.InitializeAsync();
        var owner = new CurrentUserContext(); owner.SetCurrentUser(1);
        var patient = new Patient { UserId = 1, Name = "Synthetic" }; await c.InsertAsync(patient);
        var foreign = new Patient { UserId = 2, Name = "Other profile" }; await c.InsertAsync(foreign);
        var service = new GlucoseMonitoringService(db, owner);
        var value = new PatientGlucoseMonitoring { ChecksPerDay = 4, StripsPerPickup = 100, PickupIntervalDays = 30 };
        await service.SaveAsync(patient.Id, value);
        check(GlucoseMonitoringRules.Estimate(value).Contains("120") && GlucoseMonitoringRules.Estimate(value).Contains("verificar"), "Health: 30-day estimate and informative access note");
        value.VariableFrequency = true; check(!GlucoseMonitoringRules.Estimate(value).Contains("120"), "Health: variable frequency suppresses estimate");
        await service.SaveAsync(patient.Id, value);
        check(await c.Table<PatientGlucoseMonitoring>().CountAsync() == 1, "Health: monitoring update preserves one patient record");
        await service.AddPickupAsync(patient.Id, DateTime.Today.AddDays(-2), 50, "Synthetic pickup");
        check((await service.GetPickupsAsync(patient.Id)).Single().Quantity == 50, "Health: actual pickup history independent of routine");
        var failed = false; try { await service.AddPickupAsync(patient.Id, DateTime.Today, -1, ""); } catch(ArgumentException) { failed = true; }
        check(failed, "Health: negative quantities rejected");
        failed = false; try { await service.SaveAsync(foreign.Id, value); } catch(InvalidOperationException) { failed = true; }
        check(failed, "Health: foreign profile writes rejected");
        failed = false; try { await c.InsertAsync(new PatientStripPickup { UserId = 2, PatientId = patient.Id, PickupDate = DateTime.Today, Quantity = 1 }); } catch(SQLiteException) { failed = true; }
        check(failed, "Health: composite SQLite foreign key rejects cross-profile pickup");
        owner.SetCurrentUser(2);
        check(await service.GetAsync(patient.Id) is null && (await service.GetPickupsAsync(patient.Id)).Count == 0, "Health: profile reads isolated");
        await c.CloseAsync();
        c = new SQLiteAsyncConnection(Path.Combine(directory, "health.db"));
        check((await c.Table<PatientGlucoseMonitoring>().ToListAsync()).Single().ChecksPerDay == 4 && await c.Table<PatientStripPickup>().CountAsync() == 1, "Health: monitoring and history survive connection restart");
        await c.CloseAsync();


        var upgrade = new SQLiteAsyncConnection(Path.Combine(directory, "health-upgrade6.db"));
        await new DatabaseService(upgrade).InitializeAsync();
        var legacyPerson = new Patient { UserId = 1, Name = "Preserved diabetes", Observacao = "Original health history" };
        await upgrade.InsertAsync(legacyPerson);
        await upgrade.InsertAsync(new PatientInsulinDependency { UserId = 1, PatientId = legacyPerson.Id });
        await upgrade.RunInTransactionAsync(conn =>
        {
            conn.Execute("DROP TABLE PatientInfectiousFollowUp");
            conn.Execute("DROP TABLE PatientStripPickup"); conn.Execute("DROP TABLE PatientGlucoseMonitoring");
            conn.Execute("DELETE FROM SchemaMigration WHERE Version>=7"); conn.Execute("DELETE FROM SchemaPreservation WHERE MigrationVersion>=7");
            conn.Execute("PRAGMA user_version=6");
        });
        await new ACS_View.Infrastructure.Data.Migrations.SchemaMigrator(upgrade).RunAsync();
        check((await upgrade.FindAsync<Patient>(legacyPerson.Id)).Observacao == "Original health history" && await upgrade.Table<PatientInsulinDependency>().CountAsync() == 1,
            "Health: incremental version 6 to 7 preserves patient and insulin history");
        check(await upgrade.Table<PatientGlucoseMonitoring>().CountAsync() == 0 && await upgrade.Table<PatientStripPickup>().CountAsync() == 0,
            "Health: upgrade adds optional tables without inventing monitoring data");
        await upgrade.CloseAsync();

        var active = true;
        using var session = new PatientHealthSession(1, 1, () => active);
        var calls = 0; var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> Load(CancellationToken ct) { Interlocked.Increment(ref calls); return release.Task; }
        var first = session.GetAsync("diabetes", Load); var second = session.GetAsync("diabetes", Load);
        release.SetResult(42); check(await first == 42 && await second == 42 && calls == 1, "Health: preload and open share an in-flight operation");
        check(await session.GetAsync("diabetes", Load) == 42 && calls == 1, "Health: reopen reuses loaded data");
        await session.GetAsync("visits", _ => Task.FromResult(10)); session.Invalidate("visits");
        check(await session.GetAsync("visits", _ => Task.FromResult(20)) == 20 && await session.GetAsync("diabetes", Load) == 42 && calls == 1, "Health: selective invalidation preserves unrelated module");
        var error = session.GetAsync<int>("error", _ => Task.FromException<int>(new IOException("Synthetic")));
        try { await error; } catch(IOException) { }
        check(session.State("error") == HealthLoadState.Error, "Health: module failure has independent state");
        session.Invalidate("error"); check(await session.GetAsync("error", _ => Task.FromResult(1)) == 1, "Health: explicit retry recovers failed module");
        var oldRelease = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var old = session.GetAsync("stale", _ => { entered.SetResult(); return oldRelease.Task; }); await entered.Task;
        session.Invalidate("stale"); var fresh = session.GetAsync("stale", _ => Task.FromResult(2)); oldRelease.SetResult(1);
        failed = false; try { await old; } catch(OperationCanceledException) { failed = true; }
        check(failed && await fresh == 2, "Health: invalidated in-flight result cannot overwrite fresh data");
        active = false; failed = false; try { await session.GetAsync("diabetes", Load); } catch(OperationCanceledException) { failed = true; }
        check(failed, "Health: login change rejects cached patient data");
        using var otherSession = new PatientHealthSession(2, 1, () => true);
        check(await otherSession.GetAsync("diabetes", _ => Task.FromResult(99)) == 99, "Health: patient sessions have independent caches");
        var closeRelease = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var closeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closing = otherSession.GetAsync("closing", _ => { closeEntered.SetResult(); return closeRelease.Task; }); await closeEntered.Task;
        otherSession.Dispose(); closeRelease.SetResult(3); failed = false;
        try { await closing; } catch(OperationCanceledException) { failed = true; }
        check(failed, "Health: closing discards outstanding SQLite results");
    }
}
