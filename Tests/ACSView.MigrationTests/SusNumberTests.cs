using ACS_View.Domain.Entities;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using ACS_View.ViewModels;
using SQLite;

internal static class SusNumberTests
{
    public static async Task Run(string directory, Action<bool, string> check)
    {
        var editor = new SusNumberEditorViewModel();
        editor.ApplyCommand.Execute(null);
        check(editor.HasError && editor.Numbers.Count == 0, "Empty SUS rejected by editor");
        editor.Input = "123";
        check(!editor.TryApply(), "New incomplete SUS rejected");
        editor.Input = "111.1111.1111.1111";
        editor.ApplyCommand.Execute(null);
        var first = editor.Numbers.Single();
        check(first.Number == "111111111111111" && first.IsPrimary && editor.Input == "", "Formatted SUS normalizes and first becomes primary");
        editor.Input = "111111111111111";
        check(!editor.TryApply(), "Normalized SUS duplicate rejected");
        editor.Input = "222222222222222;333333333333333";
        check(!editor.TryApply(), "SUS entry accepts only one number at a time");
        editor.Input = "222222222222222";
        check(editor.TryApply(), "Second SUS added");
        var second = editor.Numbers.Last();
        editor.PrimaryNumber = second;
        check(second.IsPrimary && !first.IsPrimary, "Primary SUS selection is exclusive");
        editor.EditCommand.Execute(first);
        check(editor.IsEditing && editor.Input == first.Number, "Edit SUS copies number into shared entry");
        editor.Input = "333333333333333";
        check(first.Number == "111111111111111", "Typing SUS edit leaves list unchanged until apply");
        editor.CancelCommand.Execute(null);
        check(!editor.IsEditing && first.Number == "111111111111111", "Cancel discards pending SUS edit");
        editor.EditCommand.Execute(first);
        editor.Input = second.Number;
        check(!editor.TryApply() && editor.IsEditing, "SUS edit cannot duplicate another number");
        editor.Input = "333333333333333";
        check(editor.TryApply() && ReferenceEquals(first, editor.Numbers.First()), "SUS edit updates the same item");
        var patient = new Patient { Name = "Synthetic SUS patient" };
        editor.ApplyTo(patient);
        check(patient.PrimarySusNumber == second.Number && patient.SusNumbers.Count == 2, "Selected primary SUS is first in patient model");
        editor.EditCommand.Execute(second);
        editor.DeleteCommand.Execute(second);
        check(first.IsPrimary && !editor.IsEditing && editor.Numbers.Count == 1, "Deleting edited primary SUS clears entry and selects remaining item");
        editor.DeleteCommand.Execute(first);
        editor.ApplyTo(patient);
        check(patient.SusNumbers.Count == 0 && editor.PrimaryNumber is null, "Deleting last SUS clears patient number set");
        const string legacy = "invalid;123;111.1111.1111.1111;111111111111111";
        patient.SusNumber = legacy;
        editor.Load(legacy);
        editor.ApplyTo(patient);
        check(patient.SusNumber == legacy && !editor.IsDirty, "Untouched SUS editor preserves raw legacy evidence");
        check(editor.Numbers.Count == 2 && editor.Numbers.First().Number == "123", "Loading preserves digit-bearing incomplete legacy SUS");
        editor.EditCommand.Execute(editor.Numbers.First());
        editor.Input = "555555555555555";
        editor.CancelCommand.Execute(null);
        editor.ApplyTo(patient);
        check(patient.SusNumber == legacy && !editor.IsDirty, "Cancelled legacy SUS edit does not normalize saved evidence");
        editor.Load("111111111111111;222222222222222");
        editor.PrimaryNumber = editor.Numbers.Last();
        editor.ApplyTo(patient);

        var c = new SQLiteAsyncConnection(Path.Combine(directory, "sus-editor.db"));
        try
        {
            var db = new DatabaseService(c);
            await db.InitializeAsync();
            var context = new CurrentUserContext();
            context.SetCurrentUser(1);
            var repository = new SQLitePatientRepository(db, context);
            await repository.InsertAsync(patient);
            var relational = new RelationalDataService(db, context);
            var rows = await relational.GetSusNumbersAsync(patient.Id);
            check(rows.Count == 2 && rows.Count(n => n.IsPrimary) == 1 && rows.Single(n => n.IsPrimary).Number == "222222222222222", "SUS editor persists both numbers and chosen primary to SQLite projection");
            var existingId = rows.Single(n => n.Number == "111111111111111").Id;
            var loaded = (await repository.GetByIdAsync(patient.Id))!;
            editor.LoadRecords(loaded.SusNumberRecords!);
            check(editor.PrimaryNumber!.Number == "222222222222222", "Reloaded SUS editor restores chosen primary");
            editor.PrimaryNumber = editor.Numbers.Last();
            editor.ApplyTo(loaded);
            await repository.UpdateAsync(loaded);
            rows = await relational.GetSusNumbersAsync(patient.Id);
            check(rows.Single(n => n.IsPrimary).Id == existingId, "Changing primary SUS preserves existing projection identity");
            editor.DeleteCommand.Execute(editor.Numbers.First());
            editor.ApplyTo(loaded);
            await repository.UpdateAsync(loaded);
            check((await relational.GetSusNumbersAsync(patient.Id)).Single().Number == "111111111111111", "SUS removal synchronizes patient and relational number set");
            check((await repository.GetListAsync("111111111111111", 0, 20, new())).TotalCount == 1 &&
                (await repository.GetListAsync("222222222222222", 0, 20, new())).TotalCount == 0,
                "Patient search follows updated SUS set after removal");
            check(await c.ExecuteScalarAsync<string>("PRAGMA integrity_check") == "ok", "SUS editor integration leaves SQLite intact");
        }
        finally { await c.CloseAsync(); }
    }
}
