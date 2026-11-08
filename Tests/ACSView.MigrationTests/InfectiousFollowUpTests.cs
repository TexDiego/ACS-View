using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.Migrations;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

internal static class InfectiousFollowUpTests
{
    public static async Task Run(string directory, Action<bool,string> check)
    {
        var path = Path.Combine(directory,"infectious.db");
        var c = new SQLiteAsyncConnection(path); var db = new DatabaseService(c); await db.InitializeAsync();
        var user = new CurrentUserContext(); user.SetCurrentUser(1);
        var person = new Patient { UserId=1, Name="Synthetic follow-up" }; await c.InsertAsync(person);
        var foreign = new Patient { UserId=2, Name="Foreign" }; await c.InsertAsync(foreign);
        await c.InsertAsync(new PatientConditions { UserId=1, PatientId=person.Id, Description=HealthConditionCatalog.Imunodeficiente });
        var service = new PatientInfectiousFollowUpService(db,user);
        check(await service.GetAsync(person.Id,"hiv") is null, "Infectious: unknown follow-up remains optional");
        var tb = new PatientInfectiousFollowUp { ConditionCode="tuberculosis", Status="Em tratamento", TreatmentStarted=DateTime.Today.AddDays(-10), LastAppointment=DateTime.Today.AddDays(-2), NextAppointment=DateTime.Today.AddDays(15), ReferenceService="Synthetic service", Notes="Synthetic TB notes" };
        await service.SaveAsync(person.Id,tb);
        var hiv = new PatientInfectiousFollowUp { ConditionCode="hiv", Status="Em acompanhamento", Notes="Synthetic HIV notes" }; await service.SaveAsync(person.Id,hiv);
        check((await service.GetAsync(person.Id,"tuberculosis"))?.NextAppointment==tb.NextAppointment && (await service.GetAsync(person.Id,"hiv"))?.Notes==hiv.Notes,"Infectious: TB and HIV persist independently");
        tb.Notes="Updated"; await service.SaveAsync(person.Id,tb);
        check(await c.Table<PatientInfectiousFollowUp>().CountAsync()==2,"Infectious: update does not duplicate follow-up records");
        var immune = new PatientInfectiousFollowUp
        {
            ConditionCode = InfectiousFollowUpRules.Immunodeficiency,
            Status = "Em acompanhamento",
            ReferenceService = "Synthetic immunodeficiency service"
        };
        await service.SaveAsync(person.Id, immune);
        check((await service.GetAsync(person.Id, InfectiousFollowUpRules.Immunodeficiency))?.ReferenceService == immune.ReferenceService
            && (await service.GetAsync(person.Id, InfectiousFollowUpRules.Hiv))?.Notes == hiv.Notes,
            "Infectious: immunodeficiency and HIV follow-up stay independent");
        check((await c.Table<PatientConditions>().ToListAsync()).Single().Description==HealthConditionCatalog.Imunodeficiente,"Infectious: follow-up does not infer a diagnosis from immunodeficiency");
        var rejected=false;try { await service.SaveAsync(foreign.Id,hiv); }catch(InvalidOperationException){rejected=true;}
        check(rejected,"Infectious: cross-profile write rejected");
        user.SetCurrentUser(2);check(await service.GetAsync(person.Id,"hiv") is null,"Infectious: cross-profile read isolated");user.SetCurrentUser(1);
        rejected=false;try { await c.InsertAsync(new PatientInfectiousFollowUp { UserId=2,PatientId=person.Id,ConditionCode="hiv" }); }catch(SQLiteException){rejected=true;}
        check(rejected,"Infectious: composite foreign key rejects mismatched profile");
        hiv.Status="Tratamento concluído";rejected=false;try{await service.SaveAsync(person.Id,hiv);}catch(ArgumentException){rejected=true;}
        check(rejected,"Infectious: HIV does not offer a completed-treatment status");hiv.Status="Em acompanhamento";
        hiv.LastAppointment=DateTime.Today.AddDays(1);rejected=false;try{await service.SaveAsync(person.Id,hiv);}catch(ArgumentException){rejected=true;}
        check(rejected,"Infectious: future completed appointment rejected");hiv.LastAppointment=null;
        await c.CloseAsync(); c=new SQLiteAsyncConnection(path);
        check((await c.Table<PatientInfectiousFollowUp>().ToListAsync()).Single(x=>x.ConditionCode=="tuberculosis").Notes=="Updated","Infectious: restart preserves treatment and observations");
        await c.CloseAsync();
        var upgrade=new SQLiteAsyncConnection(Path.Combine(directory,"infectious-upgrade7.db"));await new DatabaseService(upgrade).InitializeAsync();
        await upgrade.InsertAsync(new Patient { UserId=1, Name="Legacy" });
        await upgrade.InsertAsync(new PatientConditions { UserId=1, PatientId=1, Description=HealthConditionCatalog.Imunodeficiente });
        await upgrade.RunInTransactionAsync(conn=>
        {
            conn.Execute("DROP TABLE PatientInfectiousFollowUp");conn.Execute("DELETE FROM HealthConditionDefinition WHERE Code='hiv'");
            conn.Execute("DELETE FROM SchemaMigration WHERE Version>=8");conn.Execute("DELETE FROM SchemaPreservation WHERE MigrationVersion>=8");conn.Execute("PRAGMA user_version=7");
        });
        await new SchemaMigrator(upgrade).RunAsync();
        check(await upgrade.ExecuteScalarAsync<int>("PRAGMA user_version")==SchemaMigrator.CurrentVersion && await upgrade.Table<PatientInfectiousFollowUp>().CountAsync()==0,"Infectious: version 7 upgrade adds optional follow-up without invented records");
        check(await upgrade.ExecuteScalarAsync<int>("SELECT Id FROM HealthConditionDefinition WHERE Code='insulin'")==16 && await upgrade.ExecuteScalarAsync<int>("SELECT Id FROM HealthConditionDefinition WHERE Code='immunodeficiency'")==15,"Infectious: historic condition identities remain stable");
        check((await upgrade.Table<PatientConditions>().ToListAsync()).Single().Description==HealthConditionCatalog.Imunodeficiente,"Infectious: upgrade does not relabel immunodeficiency as HIV");

        await upgrade.RunInTransactionAsync(conn =>
        {
            conn.Execute("DROP TABLE PatientInfectiousFollowUp");
            InfectiousFollowUpSchema.Create(conn);
            conn.Execute("INSERT INTO PatientInfectiousFollowUp (Id, UserId, PatientId, ConditionCode, Notes) VALUES (5, 1, 1, 'hiv', 'Preserve legacy HIV')");
            conn.Execute("UPDATE sqlite_sequence SET seq=20 WHERE name='PatientInfectiousFollowUp'");
            conn.Execute("DELETE FROM SchemaMigration WHERE Version>=9");
            conn.Execute("DELETE FROM SchemaPreservation WHERE MigrationVersion>=9");
            conn.Execute("PRAGMA user_version=8");
        });
        await new SchemaMigrator(upgrade).RunAsync();
        var preserved = (await upgrade.Table<PatientInfectiousFollowUp>().ToListAsync()).Single();
        check(preserved.Id == 5 && preserved.ConditionCode == InfectiousFollowUpRules.Hiv && preserved.Notes == "Preserve legacy HIV",
            "Infectious: version 8 upgrade preserves HIV rows without creating immunodeficiency data");
        await upgrade.ExecuteAsync("INSERT INTO PatientInfectiousFollowUp (UserId, PatientId, ConditionCode) VALUES (1, 1, 'immunodeficiency')");
        check(await upgrade.ExecuteScalarAsync<int>("SELECT Id FROM PatientInfectiousFollowUp WHERE ConditionCode='immunodeficiency'") == 21,
            "Infectious: version 8 upgrade accepts the new condition and preserves the identity sequence");
        await upgrade.CloseAsync();
    }
}
