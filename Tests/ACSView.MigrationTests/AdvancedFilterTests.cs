using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;

internal static class AdvancedFilterTests
{
    public static async Task Run(string directory, Action<bool, string> check)
    {
        var c = new SQLiteAsyncConnection(Path.Combine(directory, "advanced-filters.db"));
        try
        {
            var db = new DatabaseService(c);
            await db.InitializeAsync();
            var context = new CurrentUserContext();
            context.SetCurrentUser(1);
            var repository = new SQLitePatientRepository(db, context);
            var missing = new Patient { Name = "Ana ausente", MotherName = " \t\r\n", FatherName = " " };
            var complete = new Patient { Name = "Bruna completa", MotherName = "Maria", FatherName = "João", SusNumber = "111111111111111", Sexo = "Feminino" };
            var secondary = new Patient { Name = "Carla secundário", MotherName = "Maria" };
            var legacy = new Patient { Name = "Dora legado" };
            var inactive = new Patient { Name = "Eva inativa", IsActive = false };
            foreach (var patient in new[] { missing, complete, secondary, legacy, inactive })
            {
                patient.SearchName = ACS_View.Domain.ValueObjects.SearchTextNormalizer.Normalize(patient.Name);
                await repository.InsertAsync(patient);
            }
            var house = new House { UserId = 1, Rua = "Rua sintética" };
            await c.InsertAsync(house);
            await c.ExecuteAsync("UPDATE Patient SET HouseId=? WHERE Id=?", house.CasaId, complete.Id);
            await c.InsertAsync(new PatientPhone { PatientId = complete.Id, UserId = 1, Number = "11987654321" });
            await c.InsertAsync(new PatientSusNumber { PatientId = secondary.Id, UserId = 1, Number = "222222222222222", IsPrimary = false });
            await c.ExecuteAsync("UPDATE Patient SET SusNumber='evidência antiga' WHERE Id=?", legacy.Id);
            await c.ExecuteAsync("UPDATE Patient SET MotherName=NULL, FatherName=NULL, SusNumber=NULL WHERE Id=?", missing.Id);

            async Task Expect(PatientListFilterDto filter, params int[] ids)
            {
                var result = await repository.GetListAsync(null, 0, 100, filter);
                check(result.TotalCount == ids.Length && result.Items.Select(p => p.Id).Order().SequenceEqual(ids.Order()),
                    "Advanced filters return expected patients and total count");
            }
            await Expect(new() { WithoutSusNumber = true }, missing.Id);
            await Expect(new() { WithoutPhone = true }, missing.Id, secondary.Id, legacy.Id);
            await Expect(new() { WithoutMotherName = true }, missing.Id, legacy.Id);
            await Expect(new() { WithoutFatherName = true }, missing.Id, secondary.Id, legacy.Id);
            await Expect(new() { WithoutHouse = true }, missing.Id, secondary.Id, legacy.Id);
            var all = new PatientListFilterDto { WithoutSusNumber = true, WithoutPhone = true, WithoutMotherName = true, WithoutFatherName = true, WithoutHouse = true };
            await Expect(all, missing.Id);
            var clone = all.Clone();
            check(clone.HasListFilters && clone.WithoutSusNumber && clone.WithoutPhone && clone.WithoutMotherName && clone.WithoutFatherName && clone.WithoutHouse,
                "Popup filter clone retains all advanced choices");
            clone.WithoutSusNumber = false;
            check(all.WithoutSusNumber && !new PatientListFilterDto().HasListFilters, "Cloned filter is independent and reset clears advanced options");
            foreach (var filter in new PatientListFilterDto[] { new() { WithoutSusNumber = true }, new() { WithoutPhone = true }, new() { WithoutMotherName = true }, new() { WithoutFatherName = true }, new() { WithoutHouse = true } })
                check(filter.HasListFilters, "Individual advanced choice marks filter as active");
            var page = await repository.GetListAsync(null, 1, 1, new() { WithoutPhone = true });
            check(page.TotalCount == 3 && page.Items.Single().Id == secondary.Id, "Advanced filter paginates after filtering and keeps full total");
            check((await repository.GetListAsync("Ana", 0, 10, all)).Items.Single().Id == missing.Id, "Text search combines with advanced filters");
            check((await repository.GetListAsync("Bruna", 0, 10, all)).TotalCount == 0, "Search cannot bypass missing-information filters");
            await Expect(new() { WithoutPhone = true, Sexes = ["Feminino"] });
            await Expect(new() { WithoutPhone = true, FilterKey = "INACTIVE" }, inactive.Id);
            context.SetCurrentUser(2);
            await Expect(all);
            check((await repository.GetListAsync(null, 0, 100, new())).TotalCount == 0, "Advanced list remains isolated by account");
            context.SetCurrentUser(1);
            // The real correction cycle must remove a patient from the filtered list.
            await c.InsertAsync(new PatientPhone { PatientId = missing.Id, UserId = 1, Number = "11911112222", IsPrimary = true });
            await Expect(new() { WithoutPhone = true }, secondary.Id, legacy.Id);
            await c.DeleteAsync<PatientPhone>((await c.Table<PatientPhone>().FirstAsync(p => p.PatientId == missing.Id)).Id);
            await c.CloseAsync();
            await Expect(new() { WithoutPhone = true }, missing.Id, secondary.Id, legacy.Id);
            check((await repository.GetListAsync(null, 0, 100, new())).TotalCount == 4, "Cleared filters restore all active patients");
        }
        finally { await c.CloseAsync(); }
    }
}
