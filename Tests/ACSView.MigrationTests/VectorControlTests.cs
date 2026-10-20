using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.Migrations;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using ACS_View.ViewModels;
using SQLite;
using System.Diagnostics;

internal static class VectorControlTests
{
    public static async Task Run(string directory, Action<bool, string> check)
    {
        var comparer = NaturalCodeComparer.Instance;
        check(new[] { "11", "1", "3", "10", "2" }.OrderBy(x => x, comparer).SequenceEqual(new[] { "1", "2", "3", "10", "11" }), "Territory uses natural ordering");
        check(new[] { "02", "2", "01", "1" }.OrderBy(x => x, comparer).Distinct().Count() == 4 && comparer.Compare("999999999999999999999", "1000000000000000000000") < 0, "Natural codes preserve zeros and support long identifiers");
        foreach (var value in new[] { "450", "450A", "450-B", " 450A " }) check(TerritorialRules.NumericPart(value) == 450, $"Numeric prefix: {value}");
        foreach (var value in new[] { "S/N", "Fundos", "", "A450", "99999999999999999999" }) check(TerritorialRules.NumericPart(value) == null, $"No valid numeric prefix: {value}");
        check(new[] { "100", "102", "104", "106" }.Where(n => TerritorialRules.MatchesNumber(n, 102, 105, HouseParity.All)).SequenceEqual(new[] { "102", "104" }), "Inclusive numeric interval");
        check(TerritorialRules.MatchesNumber("450-B", 440, 460, HouseParity.Even) && !TerritorialRules.MatchesNumber("450-B", null, null, HouseParity.Odd) && !TerritorialRules.MatchesNumber("S/N", 1, null, HouseParity.All), "Parity and nonnumeric interval exclusions");
        var sortedRows = VectorControlHierarchy.Build(new[] {
            new House { CasaId = 1, SetorCensitario = "10", NumeroQuadra = "1", Rua = "A", NumeroCasa = "10" },
            new House { CasaId = 2, SetorCensitario = "2", NumeroQuadra = "10", Rua = "A", NumeroCasa = "2" },
            new House { CasaId = 3, SetorCensitario = "2", NumeroQuadra = "2", Rua = "A", NumeroCasa = "1" }
        });
        check(sortedRows.Where(r => r.Kind == VectorRowKind.Sector).Select(r => r.Title).SequenceEqual(new[] { "Setor 2", "Setor 10" }) && sortedRows.Where(r => r.HouseId > 0).Select(r => r.HouseId).SequenceEqual(new[] { 3, 2, 1 }), "Flattened hierarchy orders sector, block and address naturally");
        var selection = new TerritorialSelection();
        selection.Add("A", 1, 3, HouseParity.All, [1, 2, 3]);
        selection.Add("B", null, null, HouseParity.All, [4, 5]);
        check(selection.SelectedIds.SetEquals([1, 2, 3, 4, 5]), "Selection accumulates across streets");
        selection.Add("C", null, null, HouseParity.All, [2, 3, 4]);
        selection.Add("C", null, null, HouseParity.All, [2, 3, 4]);
        check(selection.SelectedIds.Count == 5 && selection.Segments.Count == 3, "Duplicate residences and repeated identical segments do not duplicate");
        selection.Remove(selection.Segments[0].Id);
        check(selection.SelectedIds.SetEquals([2, 3, 4, 5]), "Removing segment preserves overlapping memberships");
        selection.Toggle(2); selection.Remove(selection.Segments[0].Id);
        check(selection.SelectedIds.SetEquals([3, 4]), "Manual deselection survives segment changes");
        selection.Toggle(2); selection.Remove(selection.Segments[0].Id);
        check(selection.SelectedIds.SetEquals([2]), "Explicit individual selection survives removal of segments");

        var path = Path.Combine(directory, "vector-control.db");
        var c = new SQLiteAsyncConnection(path);
        try
        {
            var db = new DatabaseService(c); await db.InitializeAsync();
            var context = new CurrentUserContext(); context.SetCurrentUser(1);
            var repository = new SQLiteHouseRepository(db, context); var service = new HouseService(repository);
            await c.ExecuteAsync("CREATE TRIGGER RejectNewVector BEFORE INSERT ON TerritorialAssignment WHEN NEW.CensusSector='fail' BEGIN SELECT RAISE(ABORT,'Synthetic creation failure'); END");
            var failedHouse = new House { Rua = "Falha", Bairro = "Centro", Cidade = "Cidade", Estado = "SP", SetorCensitario = "fail", NumeroQuadra = "1" };
            await Reject(() => service.SaveHouseAsync(failedHouse), check, "Residence and initial classification insert are atomic");
            check(failedHouse.CasaId == 0 && await c.Table<House>().CountAsync() == 0, "Failed insert does not publish an identity or leave an orphan residence");
            await c.ExecuteAsync("DROP TRIGGER RejectNewVector");
            var homes = new List<House>();
            foreach (var number in new[] { "100", "102", "104", "106", "450A", "450-B", "S/N", "Fundos" })
            {
                var house = new House { Rua = "João Pessoa", TipoLogradouro = "Rua", NumeroCasa = number, Bairro = "Centro", Cidade = "Cidade", Estado = "SP", CEP = "12345678", Complemento = "Original" };
                await service.SaveHouseAsync(house); homes.Add(house);
            }
            var classified = homes[0]; classified.SetorCensitario = "0001"; classified.NumeroQuadra = "12A";
            await service.UpdateHouseAsync(classified);
            check((await c.Table<TerritorialAssignment>().Where(t => t.HouseId == classified.CasaId && t.ValidTo == null).FirstAsync()).BlockNumber == "12A", "Individual editing writes the existing territory history");
            var legacyHistory = new RelationalDataService(db, context);
            await legacyHistory.AssignTerritoryAsync(new TerritorialAssignment { HouseId = classified.CasaId, CensusSector = "0001", BlockNumber = "12A", AreaCode = "Area-preserved", SectorCode = "Operation-preserved" });

            // Reconstruct version 4 using a synthetic existing database; preserve exact old fields and revisions.
            await c.ExecuteAsync("DROP INDEX IX_House_User_Active_Territory");
            await c.ExecuteAsync("ALTER TABLE House DROP COLUMN SetorCensitario");
            await c.ExecuteAsync("ALTER TABLE House DROP COLUMN NumeroQuadra");
            await c.ExecuteAsync("DELETE FROM SchemaMigration WHERE Version=5");
            await c.ExecuteAsync("DELETE FROM SchemaPreservation WHERE MigrationVersion=5");
            await c.ExecuteAsync("PRAGMA user_version=4");
            var revision = await c.ExecuteScalarAsync<int>("SELECT RowRevision FROM House WHERE CasaId=?", classified.CasaId);
            await Reject(() => new SchemaMigrator(c) { BeforeValidation = version => { if (version == 5) throw new InvalidOperationException("Injected migration failure"); } }.RunAsync(), check, "Interrupted territory migration rolls back");
            check(await c.ExecuteScalarAsync<int>("PRAGMA user_version") == 4 && (await c.GetTableInfoAsync("House")).All(column => column.Name != "SetorCensitario"), "Failed territory migration leaves version 4 schema intact");
            var migrator = new SchemaMigrator(c); await migrator.RunAsync();
            var migrated = await repository.GetAllAsync();
            check(migrated.Count == 8 && migrated.All(h => h.Complemento == "Original"), "Version 4 upgrade preserves existing residence IDs and address data");
            check(migrated.Where(h => h.CasaId != classified.CasaId).All(h => h.SetorCensitario == null && h.NumeroQuadra == null), "Unclassified legacy residences remain null");
            check(migrated.Single(h => h.CasaId == classified.CasaId).SetorCensitario == "0001" && migrated.Single(h => h.CasaId == classified.CasaId).RowRevision == revision, "Existing active classification migrates preserving zeros and original revision");
            check(migrator.BackupPath != null && File.Exists(migrator.BackupPath), "Territory migration creates a recovery snapshot");
            await new SchemaMigrator(c).RunAsync();
            check(await c.ExecuteScalarAsync<int>("PRAGMA user_version") == 5, "Territory migration is idempotent");
            var results = await service.SearchTerritorialAsync("JOAO pessoa", 102, 105, HouseParity.All);
            check(results.Select(h => h.NumeroCasa).SequenceEqual(new[] { "102", "104" }), "SQLite street search ignores accents and case with numeric filtering");
            check((await service.SearchTerritorialAsync("Rua João Pessoa", 440, 460, HouseParity.Even)).Count == 2, "Street type, suffix numbers and parity work together");
            await Reject(() => service.SearchTerritorialAsync("", null, null, HouseParity.All), check, "Empty street rejected");
            await Reject(() => service.SearchTerritorialAsync("Rua", 105, 100, HouseParity.All), check, "Reversed interval rejected");
            var reviewed = await service.GetTerritorialSelectionAsync(homes.Take(3).Select(h => h.CasaId).ToArray());
            var beforeVersion = DataChangeTracker.HousesVersion;
            check(await service.ApplyTerritoryAsync(reviewed, "0002", "02B", true) == 2, "Batch updates only residences with neither classification field");
            check((await repository.GetByIdAsync(classified.CasaId))!.SetorCensitario == "0001" && DataChangeTracker.HousesVersion > beforeVersion, "Existing classification retained and version invalidated");
            reviewed = await service.GetTerritorialSelectionAsync(homes.Take(3).Select(h => h.CasaId).ToArray());
            check(await service.ApplyTerritoryAsync(reviewed, "0003", "017", false) == 3, "Explicit overwrite applies to every selected residence");
            var currentHistory = await c.Table<TerritorialAssignment>().Where(t => t.HouseId == classified.CasaId && t.ValidTo == null).FirstAsync();
            check(currentHistory.CensusSector == "0003" && currentHistory.AreaCode == "Area-preserved" && currentHistory.SectorCode == "Operation-preserved", "Batch preserves operational territory codes and maintains one coherent active history");
            await Reject(() => service.ApplyTerritoryAsync(reviewed, "9", "9", false), check, "Stale review rejected before any writes");
            reviewed = await service.GetTerritorialSelectionAsync(homes.Take(3).Select(h => h.CasaId).ToArray());
            await c.ExecuteAsync($"CREATE TRIGGER RejectVector BEFORE UPDATE OF SetorCensitario ON House WHEN NEW.CasaId={homes[1].CasaId} BEGIN SELECT RAISE(ABORT,'Synthetic failure'); END");
            var historyCount = await c.Table<TerritorialAssignment>().CountAsync();
            await Reject(() => service.ApplyTerritoryAsync(reviewed, "fail", "9", false), check, "Injected mid-batch failure rolls back");
            check((await repository.GetByIdAsync(homes[0].CasaId))!.SetorCensitario == "0003" && await c.Table<TerritorialAssignment>().CountAsync() == historyCount, "Rollback restores residence fields and territory history");
            await c.ExecuteAsync("DROP TRIGGER RejectVector");
            context.SetCurrentUser(2);
            check((await service.SearchTerritorialAsync("joao", null, null, HouseParity.All)).Count == 0, "Street search respects account boundary");
            await Reject(() => service.GetTerritorialSelectionAsync([homes[0].CasaId]), check, "Review cannot include another account");
            await Reject(() => service.ApplyTerritoryAsync(reviewed, "9", "9", false), check, "Batch cannot modify another account");
            context.SetCurrentUser(1);
            await IndividualEditor(service, homes[0].CasaId, check);
            await c.InsertAsync(new Patient { UserId = 1, Name = "João da Busca", SearchName = SearchTextNormalizer.Normalize("João da Busca"), HouseId = homes[1].CasaId });
            await ViewModels(service, homes, check);
            var partial = (await repository.GetByIdAsync(homes[7].CasaId))!; partial.SetorCensitario = "partial"; await service.UpdateHouseAsync(partial);
            var partialReview = await service.GetTerritorialSelectionAsync([partial.CasaId]);
            check(await service.ApplyTerritoryAsync(partialReview, "overwrite", "9", true) == 0 && (await service.GetHouseByIdAsync(partial.CasaId))!.SetorCensitario == "partial", "Only-unclassified mode also protects partially classified records");
            check(TerritorialRules.IsClassified(partial) && !TerritorialRules.IsComplete(partial), "Partial classification is protected against silent overwrite");
            var rows = VectorControlHierarchy.Build(await service.GetTerritorialHousesAsync());
            check(rows.Any(r => r.Title == "Não classificados") && rows.Any(r => r.HouseId == partial.CasaId && r.Title.Contains("partial")), "Hierarchy shows unclassified and partial classification evidence");
            check(rows.Count(r => r.Kind == VectorRowKind.Residence) == homes.Count + 1, "Hierarchy displays each residence exactly once");
            var stopwatch = Stopwatch.StartNew();
            var large = Enumerable.Range(1, 10000).Select(i => new House { CasaId = i, Rua = "Rua", NumeroCasa = i.ToString(), SetorCensitario = (i % 20).ToString(), NumeroQuadra = (i % 100).ToString() }).ToArray();
            var largeRows = VectorControlHierarchy.Build(large); stopwatch.Stop();
            check(largeRows.Count(r => r.Kind == VectorRowKind.Residence) == 10000, $"Synthetic hierarchy: 10,000 residences in {stopwatch.ElapsedMilliseconds} ms (no device rendering claim)");
            var batchHouses = Enumerable.Range(1, 2000).Select(i => new House { UserId = 1, Rua = "Desempenho", NumeroCasa = i.ToString(), SearchRua = "desempenho" }).ToList();
            await c.RunInTransactionAsync(connection => connection.InsertAll(batchHouses));
            var batchReview = await service.GetTerritorialSelectionAsync(batchHouses.Select(h => h.CasaId).ToArray());
            stopwatch.Restart();
            var batchCount = await service.ApplyTerritoryAsync(batchReview, "0009", "200", false);
            stopwatch.Stop();
            check(batchCount == 2000, $"Synthetic SQLite batch: 2,000 residences and history in {stopwatch.ElapsedMilliseconds} ms, one transaction");
            await c.CloseAsync();
            c = new SQLiteAsyncConnection(path); await new DatabaseService(c).InitializeAsync();
            check((await c.FindAsync<House>(homes[0].CasaId)).SetorCensitario != null && await c.ExecuteScalarAsync<string>("PRAGMA integrity_check") == "ok" && (await c.QueryAsync<Fk>("PRAGMA foreign_key_check")).Count == 0, "Close/reopen persists classification with valid database integrity");
        }
        finally { await c.CloseAsync(); }
    }

    private static async Task IndividualEditor(HouseService service, int houseId, Action<bool, string> check)
    {
        var editor = new AddHouseViewModel(service, new FakeCep());
        await editor.LoadHouseAsync(houseId);
        var revision = editor.HouseModel.RowRevision;
        var sector = editor.HouseModel.SetorCensitario;
        var cepCommand = (Command)editor.SearchCEP;
        cepCommand.Execute(null); await cepCommand.Execution;
        check(editor.HouseModel.SetorCensitario == sector && editor.HouseModel.NumeroQuadra == "017" && editor.HouseModel.RowRevision == revision,
            "CEP lookup preserves territory and optimistic revision in the actual editor");
        editor.HouseModel.SetorCensitario = "0003";
        var save = (Command)editor.SalvarCommand; save.Execute(null); await save.Execution;
        check((await service.GetHouseByIdAsync(houseId))!.SetorCensitario == "0003" && editor.HouseModel.CasaId == 0, "Individual editor persists optional territory through existing save command");
        editor.HouseModel = new House { Rua = "Nova residência", CEP = "12345678", Bairro = "Centro", Cidade = "Cidade", Estado = "SP" };
        save = (Command)editor.SalvarCommand; save.Execute(null); await save.Execution;
        check((await service.GetTerritorialHousesAsync()).Any(h => h.Rua == "Nova Residência" && h.SetorCensitario == null && h.NumeroQuadra == null), "Individual creation remains valid without territory");
        await editor.LoadHouseAsync(houseId);
        var stale = editor.HouseModel;
        var changed = (await service.GetHouseByIdAsync(houseId))!; changed.Complemento = "Newer edit"; await service.UpdateHouseAsync(changed);
        save = (Command)editor.SalvarCommand; save.Execute(null); await save.Execution;
        check(ReferenceEquals(editor.HouseModel, stale), "Failed stale save preserves the user's form instead of clearing it");
    }
    private sealed class FakeCep : ACS_View.Application.Interfaces.ICepService
    {
        public Task<House?> GetAddressByCepAsync(string cep) => Task.FromResult<House?>(new House { CEP = cep, Rua = "João Pessoa", TipoLogradouro = "Rua", Bairro = "Centro", Cidade = "Cidade", Estado = "SP" });
    }

    private static async Task ViewModels(HouseService service, List<House> homes, Action<bool, string> check)
    {
        var vm = new VetorialControlOptionsViewModel(service) { StreetQuery = "joao", StartNumber = "102", EndNumber = "104", TargetCensusSector = "0020", TargetBlock = "A" };
        await vm.SearchCommand.ExecuteAsync(null); vm.SelectAllResultsCommand.Execute(null);
        check(vm.SelectedCount == 2 && vm.SelectionSegments.Count == 1, "Configuration commands select the searched interval");
        vm.StartNumber = "450"; vm.EndNumber = "450"; await vm.SearchCommand.ExecuteAsync(null);
        check(vm.SelectedCount == 2, "New search keeps previous selection");
        vm.SelectAllResultsCommand.Execute(null); vm.SearchResults[0].IsSelected = false;
        check(vm.SelectedCount == 3 && vm.SelectionSegments.Count == 2, "Checkbox deselects one result while preserving the accumulated batch");
        BaseViewModel.ActionChoices.Enqueue("Alterar todas as selecionadas"); BaseViewModel.Confirmations.Enqueue(false);
        await vm.ApplyBatchCommand.ExecuteAsync(null);
        check(vm.SelectedCount == 3 && (await service.GetHouseByIdAsync(homes[1].CasaId))!.SetorCensitario == "0003", "Cancel review preserves selection and database");
        BaseViewModel.ActionChoices.Enqueue("Alterar todas as selecionadas"); BaseViewModel.Confirmations.Enqueue(true);
        await vm.ApplyBatchCommand.ExecuteAsync(null);
        check(vm.SelectedCount == 0 && vm.SearchResults.Count == 0 && (await service.GetHouseByIdAsync(homes[1].CasaId))!.SetorCensitario == "0020", "Confirmed review applies and clears batch");
        var vector = new VetorialControlViewModel(service); await vector.LoadAsync();
        vector.OptionsCommand.Execute(null);
        check(BaseViewModel.LastRoute == "vetorialcontroloptions", "Existing options route is preserved");
        vector.FamilyCommand.Execute(vector.Items.First(r => r.HouseId > 0));
        check(BaseViewModel.LastRoute == "families" && BaseViewModel.LastParameters!.ContainsKey("id"), "Hierarchy opens the existing residence families route");
        var loaded = vector.Items; await vector.LoadAsync();
        check(ReferenceEquals(loaded, vector.Items), "Unchanged lifecycle avoids rebuilding the hierarchy");
        DataChangeTracker.MarkHousesChanged(); await vector.LoadAsync();
        check(!ReferenceEquals(loaded, vector.Items), "Return after house change refreshes hierarchy");
        vector.SearchText = "JOAO DA BUSCA"; vector.CancelPendingSearch(); await vector.LoadAsync();
        check(vector.Items.Where(r => r.HouseId > 0).Select(r => r.HouseId).SequenceEqual(new[] { homes[1].CasaId }), "Vector search finds patients with the same normalized criteria as residences");
        foreach (var query in new[] { "102", "joao pessoa", "Original" })
        {
            vector.SearchText = query; vector.CancelPendingSearch(); await vector.LoadAsync();
            var expected = await service.GetHouseListAsync(query, 0, 100);
            check(vector.Items.Where(r => r.HouseId > 0).Select(r => r.HouseId).ToHashSet().SetEquals(expected.Items.Select(h => h.CasaId)), "Vector address search matches residences page: " + query);
        }
        vector.SearchText = "0020"; vector.CancelPendingSearch(); await vector.LoadAsync();
        check(!vector.Items.Any(), "Default address search does not interpret numeric input as a census sector");
        vector.SearchModeIndex = 1; vector.CancelPendingSearch(); await vector.LoadAsync();
        check(vector.Items.Count(r => r.HouseId > 0) == 3 && vector.SearchPlaceholder == "Buscar setor censitário", "Explicit sector search finds only that sector and labels the input");
        vector.SearchModeIndex = 2; vector.SearchText = "017"; vector.CancelPendingSearch(); await vector.LoadAsync();
        check(vector.Items.Where(r => r.HouseId > 0).Select(r => r.HouseId).SequenceEqual(new[] { homes[0].CasaId }) && vector.SearchPlaceholder == "Buscar quadra", "Explicit block search preserves leading zeros and labels the input");
        vector.SearchText = "102"; vector.CancelPendingSearch(); await vector.LoadAsync();
        check(!vector.Items.Any(), "Block search does not match a residence number");
        vector.SearchModeIndex = 0; vector.CancelPendingSearch(); await vector.LoadAsync();
        check(vector.Items.Any(r => r.HouseId == homes[1].CasaId), "Changing to address mode searches the residence number");
        vector.SearchText = ""; vector.CancelPendingSearch(); await vector.LoadAsync();
        check(vector.Items.Count(r => r.HouseId > 0) == homes.Count + 1, "Clearing vector search restores all residences and hierarchy");
        vector.SearchText = "no-such-street"; vector.SearchText = "102";
        await Task.Delay(400);
        check(vector.Items.Where(r => r.HouseId > 0).Select(r => r.HouseId).SequenceEqual(new[] { homes[1].CasaId }), "Debounced search displays only the latest typed query");
        vector.CancelPendingSearch();
        vm.SearchResults = Array.Empty<TerritorialSearchResult>();
        DataChangeTracker.MarkSessionChanged(); vm.OnAppearing();
        check(vm.SelectedCount == 0 && vm.TargetCensusSector == "", "Session change clears territorial configuration state");
    }
    private sealed class Fk { public string table { get; set; } = ""; }
    private static async Task Reject(Func<Task> action, Action<bool, string> check, string label)
    {
        try { await action(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or SQLiteException) { check(true, label); return; }
        throw new InvalidOperationException("Expected rejection: " + label);
    }
}
