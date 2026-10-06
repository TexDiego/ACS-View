using ACS_View.Domain.Entities;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

SQLitePCL.Batteries_V2.Init();
var checks = 0;
await RunScenario(legacy: true);
await RunScenario(legacy: false);
Console.WriteLine($"{checks} Bolsa Familia regression checks passed.");

async Task RunScenario(bool legacy)
{
    var path = Path.Combine(Path.GetTempPath(), $"acs-bolsa-{Guid.NewGuid():N}.db");
    var connection = new SQLiteAsyncConnection(path);
    try
    {
        await connection.CreateTableAsync<Patient>();
        await connection.CreateTableAsync<PatientBolsaFamilia>();
        if (legacy)
            await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN BolsaFamilia INTEGER NOT NULL DEFAULT 0");

        var removed = new Patient { UserId = 1, Name = "Beneficiario removido" };
        var retained = new Patient { UserId = 1, Name = "Beneficiario mantido" };
        var otherUser = new Patient { UserId = 2, Name = "Outra conta" };
        await connection.InsertAsync(removed);
        await connection.InsertAsync(retained);
        await connection.InsertAsync(otherUser);
        if (legacy)
            await connection.ExecuteAsync("UPDATE Patient SET BolsaFamilia = 1");
        else
            await connection.InsertAsync(new PatientBolsaFamilia { UserId = 1, PatientId = removed.Id, ResponsiblePatientId = removed.Id });

        // An existing benefit's responsible person and NIS must survive the migration.
        await connection.InsertAsync(new PatientBolsaFamilia
        {
            UserId = 1, PatientId = retained.Id, ResponsiblePatientId = removed.Id, NisNumber = "12345678901"
        });
        var context = new CurrentUserContext();
        context.SetCurrentUser(1);
        var database = new DatabaseService(connection);
        await database.InitializeAsync();
        var repository = new SQLitePatientBolsaFamiliaRepository(database, context);
        Check(await repository.GetByPatientIdAsync(removed.Id) is not null, "Initial benefit is available");
        var existing = await repository.GetByPatientIdAsync(retained.Id);
        Check(existing?.NisNumber == "12345678901" && existing.ResponsiblePatientId == removed.Id,
            "Migration preserves existing NIS and responsible person");

        // Same repository operation used by Save when the registration checkbox is cleared.
        await repository.DeleteByPatientIdAsync(removed.Id);
        await repository.DeleteByPatientIdAsync(otherUser.Id);
        Check(await repository.GetByPatientIdAsync(removed.Id) is null, "Registration removes benefit");
        await connection.CloseAsync();

        for (var restart = 0; restart < 2; restart++)
        {
            connection = new SQLiteAsyncConnection(path);
            database = new DatabaseService(connection);
            await database.InitializeAsync();
            repository = new SQLitePatientBolsaFamiliaRepository(database, context);
            Check(await repository.GetByPatientIdAsync(removed.Id) is null, "Restart does not recreate removed benefit");
            var groups = await repository.GetGroupsAsync();
            Check(groups.SelectMany(g => g.Beneficiaries).All(b => b.PatientId != removed.Id),
                "Removed patient stays out of benefit list after restart");
            Check(await repository.GetByPatientIdAsync(retained.Id) is not null, "Restart preserves current beneficiaries");
            if (legacy)
                Check(await connection.Table<PatientBolsaFamilia>().CountAsync(b => b.UserId == 2 && b.PatientId == otherUser.Id) == 1,
                    "Removal is scoped to the current account");
            await connection.CloseAsync();
        }

        // Explicitly selecting the benefit again must still work.
        connection = new SQLiteAsyncConnection(path);
        database = new DatabaseService(connection);
        await database.InitializeAsync();
        repository = new SQLitePatientBolsaFamiliaRepository(database, context);
        await repository.UpsertAsync(new PatientBolsaFamilia { PatientId = removed.Id, ResponsiblePatientId = removed.Id });
        await connection.CloseAsync();
        connection = new SQLiteAsyncConnection(path);
        database = new DatabaseService(connection);
        await database.InitializeAsync();
        repository = new SQLitePatientBolsaFamiliaRepository(database, context);
        Check(await repository.GetByPatientIdAsync(removed.Id) is not null, "Explicit reactivation survives restart");
    }
    finally
    {
        await connection.CloseAsync();
        File.Delete(path);
    }
}

void Check(bool passed, string description)
{
    if (!passed) throw new InvalidOperationException(description);
    checks++;
}
