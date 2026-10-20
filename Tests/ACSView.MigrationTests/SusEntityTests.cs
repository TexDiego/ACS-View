using ACS_View.Domain.Entities;
using ACS_View.Infrastructure.Data.Migrations;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

internal static class SusEntityTests
{
    public static async Task Run(string directory, Action<bool, string> check)
    {
        var c = new SQLiteAsyncConnection(Path.Combine(directory, "sus-entity.db"));
        try
        {
            var db = new DatabaseService(c);
            await db.InitializeAsync();
            var context = new CurrentUserContext(); context.SetCurrentUser(1);
            var patients = new SQLitePatientRepository(db, context);
            var patient = new Patient { Name = "Synthetic SUS entity", SusNumber = "invalid;123;111.1111.1111.1111" };
            await patients.InsertAsync(patient);
            var other = new Patient { Name = "Synthetic no SUS" }; await patients.InsertAsync(other);
            var service = new PatientSusService(new SQLitePatientSusRepository(db, context));
            check((await service.GetNumbersAsync(other.Id)).Count == 0, "Patient without SUS has no invented entity");
            var rows = await service.GetNumbersAsync(patient.Id);
            var incompleteId = rows.Single(n => n.Number == "123").Id;
            rows.Single(n => n.Number == "123").IsPrimary = false;
            rows.Single(n => n.Number.Length == 15).IsPrimary = true;
            await service.SaveNumbersAsync(patient.Id, rows);
            check(await service.GetMainSusAsync(patient.Id) == "111111111111111", "Main SUS service returns only selected primary");
            var listed = await patients.GetListAsync("123", 0, 20, new());
            check(listed.Items.Single().PrimarySusNumber == "111111111111111", "Registers list shows selected primary even when searching secondary SUS");
            var noSusList = await patients.GetListAsync(null, 0, 20, new());
            check(noSusList.Items.Single(p => p.Id == other.Id).PrimarySusNumber == "", "Registers list has no displayed SUS for patient without numbers");
            check(await c.Table<MigrationIssue>().CountAsync(n => n.Code == "LegacyCnsEvidence" && n.Evidence == "invalid;123;111.1111.1111.1111") == 1,
                "Explicit change retains exact legacy evidence");
            await service.UpdateSusAsync(incompleteId, "222222222222222");
            check(await service.GetSusByIdAsync(incompleteId) == "222222222222222", "Editing SUS retains entity identity");
            await Reject(() => service.SaveNumbersAsync(other.Id, rows), check, "SUS IDs cannot move to another patient");
            await Reject(() => service.CreateSusAsync(other.Id, "222222222222222"), check, "New shared SUS rejected without associating another patient");
            context.SetCurrentUser(2);
            check((await service.GetNumbersAsync(patient.Id)).Count == 0 && await service.GetSusByIdAsync(incompleteId) == "", "Other account cannot read SUS records");
            await Reject(() => service.SaveNumbersAsync(patient.Id, []), check, "Other account cannot save SUS records");
            await service.DeleteSusAsync(incompleteId);
            context.SetCurrentUser(1);
            check((await service.GetNumbersAsync(patient.Id)).Count == 2, "Other account cannot delete SUS records");
            await c.ExecuteAsync("CREATE TRIGGER RejectSus BEFORE INSERT ON PatientSusNumber WHEN NEW.Number='999999999999999' BEGIN SELECT RAISE(ABORT,'Synthetic rollback'); END");
            var additions = new List<PatientSusNumber> { new() { Number = "333333333333333", IsPrimary = true }, new() { Number = "999999999999999" } };
            await Reject(() => service.SaveNumbersAsync(patient.Id, additions), check, "Failed SUS transaction rolls back");
            check(additions.All(n => n.Id == 0) && (await service.GetNumbersAsync(patient.Id)).Count == 2, "Rollback retains numbers and publishes no generated identities");
            await c.ExecuteAsync("DROP TRIGGER RejectSus");
            await service.SaveNumbersAsync(patient.Id, additions);
            (additions[0].Number, additions[1].Number) = (additions[1].Number, additions[0].Number);
            await service.SaveNumbersAsync(patient.Id, additions);
            check(await service.GetSusByIdAsync(additions[0].Id) == "999999999999999", "SUS number swap retains IDs without violating uniqueness");
            await service.SaveNumbersAsync(patient.Id, []);
            check((await patients.GetByIdAsync(patient.Id))!.SusNumbers.Count == 0, "Removing final SUS survives patient reload");

            // Recreate the immediately previous version with incomplete backfill and an extra entity.
            await c.ExecuteAsync("DELETE FROM SchemaMigration WHERE Version>=4");
            await c.ExecuteAsync("PRAGMA user_version=3");
            await c.ExecuteAsync("UPDATE Patient SET SusNumber=? WHERE Id=?", "raw;444444444444444;555555555555555", patient.Id);
            var retained = new PatientSusNumber { UserId = 1, PatientId = patient.Id, Number = "666666666666666", IsPrimary = true, OriginalOrder = 0 };
            await c.InsertAsync(retained);
            await new SchemaMigrator(c).RunAsync();
            var migrated = await service.GetNumbersAsync(patient.Id);
            check(migrated.Count == 3 && migrated.Single(n => n.IsPrimary).Id == retained.Id, "Version 3 upgrade adds missing legacy numbers and preserves existing entity and primary");
            check((await c.FindAsync<Patient>(patient.Id)).SusNumber == "raw;444444444444444;555555555555555", "SUS migration preserves legacy property verbatim");
            await new SchemaMigrator(c).RunAsync();
            check((await service.GetNumbersAsync(patient.Id)).Count == 3, "SUS migration is idempotent");
            var loaded = (await patients.GetByIdAsync(patient.Id))!;
            check(loaded.SusNumbers.Contains("666666666666666"), "Patient reads entity numbers beyond legacy text");
            loaded.Name = "Synthetic changed name"; await patients.UpdateAsync(loaded);
            check((await service.GetNumbersAsync(patient.Id)).Count == 3, "Unrelated patient edit retains all entity numbers");
            check(await c.ExecuteScalarAsync<string>("PRAGMA integrity_check") == "ok" && (await c.QueryAsync<Fk>("PRAGMA foreign_key_check")).Count == 0, "SUS integration preserves SQLite integrity and ownership references");
        }
        finally { await c.CloseAsync(); }
    }
    private static IReadOnlyList<PatientSusNumber> awaitRows(List<PatientSusNumber> rows) => rows;
    private sealed class Fk { public string table { get; set; } = ""; }
    private static async Task Reject(Func<Task> action, Action<bool, string> check, string label)
    {
        try { await action(); }
        catch (Exception e) when (e is SQLiteException or InvalidOperationException or ArgumentException) { check(true, label); return; }
        throw new InvalidOperationException("Expected rejection: " + label);
    }
}
