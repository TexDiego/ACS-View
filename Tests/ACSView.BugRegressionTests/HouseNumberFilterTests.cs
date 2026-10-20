using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using ACS_View.ViewModels;
using SQLite;

internal static class HouseNumberFilterTests
{
    public static async Task Run(string directory, Action<bool, string> check)
    {
        var c = new SQLiteAsyncConnection(Path.Combine(directory, "house-number-filter.db"));
        var db = new DatabaseService(c);
        await db.InitializeAsync();
        var context = new CurrentUserContext(); context.SetCurrentUser(1);
        var repository = new SQLiteHouseRepository(db, context);
        var service = new HouseService(repository);
        foreach (var number in Enumerable.Range(0, 85).Select(n => n.ToString()).Concat(
            new[] { "450A", "450-B", "\t14A", "\u00a016-B", "S/N", "Fundos", "", " 012A ", "2147483648", "99999999999999999999" }))
            await c.InsertAsync(new House { UserId = 1, Rua = "Alameda São João", SearchRua = SearchTextNormalizer.Normalize("Alameda São João"), SearchComplemento = "fundos", NumeroCasa = number, Complemento = "Fundos" });
        await c.InsertAsync(new House { UserId = 2, Rua = "Alameda São João", SearchRua = SearchTextNormalizer.Normalize("Alameda São João"), SearchComplemento = "fundos", NumeroCasa = "12" });
        await c.InsertAsync(new House { UserId = 1, Rua = "Outra rua", NumeroCasa = "12" });
        var removed = new House { UserId = 1, Rua = "Alameda São João", SearchRua = SearchTextNormalizer.Normalize("Alameda São João"), SearchComplemento = "fundos", NumeroCasa = "12", IsActive = false };
        await c.InsertAsync(removed);
        var all = await repository.GetAllAsync("sao joao");
        check(all.Count == 95, "Fixture loads active account residences through normalized address search");
        foreach (var filter in new[]
        {
            new HouseNumberFilterDto(), new(10, 20), new(10, null, HouseParity.Even),
            new(null, 20, HouseParity.Odd), new(null, null, HouseParity.Even),
            new(null, null, HouseParity.Odd), new(450, 450), new(0, 0)
        })
        {
            var expected = all.Where(h => TerritorialRules.MatchesNumber(h.NumeroCasa, filter.Start, filter.End, filter.Parity))
                .Select(h => h.CasaId).ToHashSet();
            var ids = new List<int>();
            for (var skip = 0; skip < Math.Max(1, expected.Count); skip += 7)
            {
                var page = await service.GetHouseListAsync("sao joao", skip, 7, numberFilter: filter);
                check(page.TotalCount == expected.Count, $"Count before pagination matches territorial rules: {filter}, offset {skip}");
                ids.AddRange(page.Items.Select(h => h.CasaId));
            }
            check(ids.Count == ids.Distinct().Count() && expected.SetEquals(ids),
                $"All pages respect number, parity, normalized search, active status and account: {filter}");
        }
        var residentHouse = all.Single(h => h.NumeroCasa == "10");
        await c.InsertAsync(new Patient { UserId = 1, Name = "Maria Silva", SearchName = "maria silva", HouseId = residentHouse.CasaId, FamilyId = 1 });
        check((await service.GetHouseListAsync("maria", 0, 30, numberFilter: new(10, 10, HouseParity.Even))).TotalCount == 1,
            "Patient search combines with inclusive number and parity");
        check((await service.GetHouseListAsync("maria", 0, 30, numberFilter: new(10, 10, HouseParity.Odd))).TotalCount == 0,
            "Parity excludes a residence even when its patient matches");
        check((await service.GetHouseListAsync("maria", 0, 30, DashboardFilterKeys.EmptyResidences, new(10, 10))).TotalCount == 0,
            "Dashboard empty-residence filter remains combined with number criteria");
        check((await service.GetHouseListAsync("fundos", 0, 100, numberFilter: new(450, 450))).TotalCount == 2,
            "Complement search includes both suffixed numbers at the inclusive limits");
        var draft = new HouseFilterPopupViewModel(new(10, 20, HouseParity.Odd));
        check(draft.StartNumber == "10" && draft.EndNumber == "20" && draft.ParityIndex == 2,
            "Reopened filter editor restores applied values");
        foreach (var bounds in new[] { ("20", "10"), ("-1", ""), ("abc", ""), ("", "2147483648"), ("1.5", "") })
        {
            draft.StartNumber = bounds.Item1; draft.EndNumber = bounds.Item2;
            check(!draft.TryCreateFilter(out _) && draft.ErrorMessage.Length > 0, $"Invalid filter rejected: {bounds}");
        }
        draft.StartNumber = draft.EndNumber = ""; draft.ParityIndex = 0;
        check(draft.TryCreateFilter(out var cleared) && cleared is { IsActive: false } && draft.ErrorMessage == "",
            "Blank limits and Todos remove number restrictions and validation errors");
        check(await c.ExecuteScalarAsync<string>("PRAGMA integrity_check") == "ok", "Filtered queries preserve database integrity");
        await c.CloseAsync();
    }
}
