using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using SQLite;
using ACS_View.ViewModels;
using ACS_View.Infrastructure.Data.Migrations;

internal static class DashboardPregnancyRegressionTests
{
    public static async Task Run(string directory, Action<bool, string> check)
    {
        var path = Path.Combine(directory, "dashboard-pregnancy.db");
        var c = new SQLiteAsyncConnection(path);
        var db = new DatabaseService(c);
        await db.InitializeAsync();
        var context = new CurrentUserContext(); context.SetCurrentUser(1);
        var empty = new House { UserId = 1 };
        var ungrouped = new House { UserId = 1 };
        var occupied = new House { UserId = 1 };
        var inactive = new House { UserId = 1 };
        await c.InsertAsync(empty); await c.InsertAsync(ungrouped);
        await c.InsertAsync(occupied); await c.InsertAsync(inactive);
        await c.InsertAsync(new House { UserId = 2 });
        var mother = new Patient { UserId = 1, Name = "Mother", HouseId = ungrouped.CasaId };
        await c.InsertAsync(mother);
        await c.InsertAsync(new Patient { UserId = 1, HouseId = occupied.CasaId, FamilyId = 1, MotherPatientId = mother.Id });
        await c.InsertAsync(new Patient { UserId = 1, HouseId = inactive.CasaId, FamilyId = 1, IsActive = false, MotherPatientId = mother.Id });
        await c.InsertAsync(new Patient { UserId = 1, MotherName = "Mother" });
        var otherMother = new Patient { UserId = 2 }; await c.InsertAsync(otherMother);
        await c.InsertAsync(new Patient { UserId = 2, MotherPatientId = otherMother.Id });
        var metrics = new DashboardMetricsService(db, context);
        var houses = new SQLiteHouseRepository(db, context);
        var summary = await metrics.GetMetricsAsync();
        var filtered = await houses.GetListAsync(null, 0, 100, DashboardFilterKeys.EmptyResidences);
        check(summary.TotalResidenciasVazias == 3 && filtered.TotalCount == 3
            && filtered.Items.Select(h => h.CasaId).ToHashSet().SetEquals([empty.CasaId, ungrouped.CasaId, inactive.CasaId]),
            "Empty metric and drill-down agree for no residents, no family and inactive residents, scoped by account");
        var pregnancy = new PregnancyService(db, context);
        check(await pregnancy.GetRegisteredChildrenCountAsync(mother.Id) == 2,
            "New pregnancy counts MotherPatientId links including inactive children without guessing by name");
        check(await pregnancy.GetRegisteredChildrenCountAsync(otherMother.Id) == 0
            && await pregnancy.GetRegisteredChildrenCountAsync(0) == 0, "Child count rejects other accounts and unsaved patients");
        await pregnancy.SaveAsync(new PatientPregnancy { PatientId = mother.Id, Status = PregnancyStatus.Active });
        await c.RunInTransactionAsync(connection => {
            foreach (var patient in connection.Table<Patient>().ToList()) RelationalStore.SyncPatient(connection, patient);
        });
        await c.CloseAsync();
        c = new SQLiteAsyncConnection(path); db = new DatabaseService(c); await db.InitializeAsync();
        pregnancy = new PregnancyService(db, context);
        check((await pregnancy.GetDetailsByPatientIdAsync(mother.Id))!.RegisteredChildrenCount == 2,
            "Saved pregnancy retains the same linked child count after closing and reopening the database");
        await c.ExecuteAsync("UPDATE Patient SET FamilyId=1 WHERE Id=?", mother.Id);
        await c.RunInTransactionAsync(connection => RelationalStore.SyncPatient(connection, connection.Find<Patient>(mother.Id)));
        check((await new DashboardMetricsService(db, context).GetMetricsAsync()).TotalResidenciasVazias == 2,
            "Adding a visible family removes the residence from empty metrics");
        check(await c.ExecuteScalarAsync<string>("PRAGMA integrity_check") == "ok", "Regression database integrity preserved");
        var houseService = new HouseService(new SQLiteHouseRepository(db, context));
        foreach (var number in new[] { "10", "12" })
            await houseService.SaveHouseAsync(new House { Rua = "Alpha", NumeroCasa = number, Bairro = "Centro", Cidade = "Cidade", Estado = "SP", CEP = "12345678" });
        var vm = new VetorialControlOptionsViewModel(houseService) { StreetQuery = "Alpha" };
        await vm.SearchCommand.ExecuteAsync(null);
        check(vm.SearchResults.Count == 2, "Territorial regression loads real SQLite results");
        var originalResults = vm.SearchResults;
        vm.SearchResults[0].IsSelected = true;
        var individualSegment = vm.SelectionSegments.Single();
        check(vm.SelectedCount == 1 && individualSegment.Summary.Contains("individual")
            && ReferenceEquals(originalResults, vm.SearchResults), "First checkbox creates a visible individual selection segment");
        vm.ToggleResultCommand.Execute(vm.SearchResults[1]);
        check(vm.SelectedCount == 2 && ReferenceEquals(individualSegment, vm.SelectionSegments.Single()),
            "Individual address taps accumulate in the same existing segment row");
        vm.SearchResults[0].IsSelected = false;
        check(vm.SelectedCount == 1 && ReferenceEquals(individualSegment, vm.SelectionSegments.Single()),
            "Deselecting one individual residence updates the existing segment");
        vm.StreetQuery = "No results";
        await vm.SearchCommand.ExecuteAsync(null);
        check(vm.SelectedCount == 1 && ReferenceEquals(individualSegment, vm.SelectionSegments.Single()),
            "Individual segment survives a search with no results");
        vm.StreetQuery = "Alpha";
        await vm.SearchCommand.ExecuteAsync(null);
        check(vm.SearchResults.Count(row => row.IsSelected) == 1, "Searching again restores individual checkboxes");
        vm.SearchResults.Single(row => row.IsSelected).IsSelected = false;
        check(vm.SelectedCount == 0 && vm.SelectionSegments.Count == 0, "Last individual deselection removes its empty segment");
        vm.SearchResults[0].IsSelected = true;
        vm.RemoveSegmentCommand.Execute(vm.SelectionSegments.Single());
        check(vm.SelectedCount == 0 && vm.SearchResults.All(row => !row.IsSelected), "Removing an individual segment clears its residences");
        vm.SelectAllResultsCommand.Execute(null);
        var segments = vm.SelectionSegments;
        var segment = segments.Single();
        var results = vm.SearchResults;
        var changes = 0;
        segments.CollectionChanged += (_, _) => changes++;
        vm.SearchResults[0].IsSelected = false;
        check(vm.SelectedCount == 1 && changes == 0 && ReferenceEquals(segments, vm.SelectionSegments)
            && ReferenceEquals(segment, vm.SelectionSegments.Single()) && ReferenceEquals(results, vm.SearchResults)
            && segment.Summary.EndsWith("1 residências"), "Checkbox updates count and summary without rebuilding header or result rows");
        vm.ToggleResultCommand.Execute(vm.SearchResults[0]);
        check(vm.SelectedCount == 2 && vm.SearchResults.All(row => row.IsSelected), "Address tap and checkbox keep the same selection");
        vm.RemoveSegmentCommand.Execute(segment);
        check(vm.SelectedCount == 1 && vm.SelectionSegments.Count == 1 && vm.SelectionSegments.Single().Summary.Contains("individual"),
            "Removing bulk segment preserves the explicit individual selection and its visible summary");
        vm.ClearSelectionCommand.Execute(null);
        check(vm.SelectedCount == 0 && vm.SearchResults.All(row => !row.IsSelected), "Clear selection updates all existing rows");
        await c.CloseAsync();
    }
}
