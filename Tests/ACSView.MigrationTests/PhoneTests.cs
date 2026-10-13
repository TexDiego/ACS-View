using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.SQLite;
using ACS_View.Infrastructure.Services;
using ACS_View.UseCases.Services;
using ACS_View.ViewModels;
using SQLite;

internal static class PhoneTests
{
    public static async Task Run(string directory, Action<bool, string> check)
    {
        var editor = new PhoneEditorViewModel();
        editor.ApplyCommand.Execute(null);
        check(editor.HasError && editor.Phones.Count == 0, "Empty telephone rejected by editor");
        editor.Input = "(11) 98765-4321";
        editor.ApplyCommand.Execute(null);
        var first = editor.Phones.Single();
        check(first.IsPrimary && editor.Input == "", "First phone becomes primary and entry clears");
        editor.Input = "11987654321";
        check(!editor.TryApply() && editor.Phones.Count == 1, "Formatted telephone duplicate rejected");
        editor.Input = "1134567890";
        check(editor.TryApply(), "Landline added");
        var second = editor.Phones.Last();
        editor.PrimaryPhone = second;
        check(second.IsPrimary && !first.IsPrimary, "Principal telephone selection is exclusive");
        editor.EditCommand.Execute(first);
        check(editor.Input == first.Number && editor.IsEditing, "Edit command copies number into entry model");
        editor.Input = "11911112222";
        check(first.Number == "(11) 98765-4321", "Typing an edit does not mutate list before applying");
        editor.CancelCommand.Execute(null);
        check(first.Number == "(11) 98765-4321" && !editor.IsEditing, "Cancel discards entry edit");
        editor.EditCommand.Execute(first);
        editor.Input = second.Number;
        check(!editor.TryApply() && editor.IsEditing, "Editing cannot create a duplicate");
        editor.Input = "11911112222";
        check(editor.TryApply() && ReferenceEquals(first, editor.Phones.First()), "Apply edits existing item without adding another");
        editor.EditCommand.Execute(second);
        editor.DeleteCommand.Execute(second);
        check(editor.Phones.Count == 1 && first.IsPrimary && !editor.IsEditing, "Deleting edited primary clears entry and selects remaining number");
        editor.DeleteCommand.Execute(first);
        check(editor.Phones.Count == 0 && editor.PrimaryPhone == null, "Deleting final phone clears primary");
        check(Phone.Format("11987654321") == "(11) 98765-4321" && Phone.Format("1134567890") == "(11) 3456-7890", "Mobile and landline display formatting");
        check(Phone.Format("+55 11 98765-4321") == "+55 11 98765-4321", "International phone retains country code");

        var connection = new SQLiteAsyncConnection(Path.Combine(directory, "phone-editor.db"));
        try
        {
            var db = new DatabaseService(connection);
            await db.InitializeAsync();
            var context = new CurrentUserContext();
            context.SetCurrentUser(1);
            var patients = new SQLitePatientRepository(db, context);
            var patient = new Patient { Name = "Synthetic phone patient", SusNumber = "111111111111111" };
            await patients.InsertAsync(patient);
            var another = new Patient { Name = "Second synthetic patient", SusNumber = "222222222222222" };
            await patients.InsertAsync(another);
            var relational = new RelationalDataService(db, context);
            var service = new PatientPhoneService(new SQLitePatientPhoneRepository(db, context, relational));
            editor.Input = "(11) 98765-4321";
            editor.TryApply();
            editor.Input = "+55 11 91234-5678";
            editor.TryApply();
            await service.SavePhones(patient.Id, editor.Phones.ToList());
            check(editor.Phones.All(p => p.Id > 0 && p.PatientId == patient.Id), "Committed phone IDs assigned to editor");
            var saved = (await service.GetPhonesByPatientId(patient.Id))!;
            check(saved.Count == 2 && saved.Count(p => p.IsPrimary) == 1, "Phone list persists with one primary");
            var originalId = saved[0].Id;
            await connection.ExecuteAsync("UPDATE PatientPhone SET Type='Celular',ContactName='Synthetic contact',Relationship='Mãe',HasWhatsApp=1,Notes='Retain' WHERE Id=?", originalId);
            editor.Load(saved);
            editor.EditCommand.Execute(editor.Phones[0]);
            editor.Input = "11933334444";
            editor.TryApply();
            editor.PrimaryPhone = editor.Phones[1];
            await service.SavePhones(patient.Id, editor.Phones.ToList());
            var row = await connection.FindAsync<PatientPhone>(originalId);
            check(row.Number == "11933334444" && row.Type == "Celular" && row.ContactName == "Synthetic contact" && row.Relationship == "Mãe" && row.HasWhatsApp && row.Notes == "Retain", "Phone edit retains identity and additional contact metadata");
            var original = editor.Phones.Select(p => p.Number).ToArray();
            editor.Phones[0].Number = original[1];
            editor.Phones[1].Number = original[0];
            await service.SavePhones(patient.Id, editor.Phones.ToList());
            check((await service.GetPhonesByPatientId(patient.Id))![0].Number == original[1], "Phone number swaps respect unique database constraint");
            await MustReject(() => service.SavePhones(patient.Id, [new Phone { Number = "11933334444" }, new Phone { Number = "(11) 93333-4444" }]));
            check((await service.GetPhonesByPatientId(patient.Id))!.Count == 2, "Rejected duplicate list preserves saved contacts");

            // A database error after delete and insert must roll back the entire list.
            await connection.ExecuteAsync("CREATE TRIGGER RejectPhone BEFORE INSERT ON PatientPhone WHEN NEW.Number='999' BEGIN SELECT RAISE(ABORT,'Synthetic rollback'); END");
            var additions = new List<Phone> { new() { Number = "11888887777", IsPrimary = true }, new() { Number = "999" } };
            await MustReject(() => service.SavePhones(patient.Id, additions));
            check(additions.All(p => p.Id == 0) && (await service.GetPhonesByPatientId(patient.Id))!.Count == 2, "Failed transaction preserves original list and does not publish generated IDs");
            await connection.ExecuteAsync("DROP TRIGGER RejectPhone");
            await service.SavePhones(patient.Id, additions);
            await service.SavePhones(patient.Id, additions);
            check((await service.GetPhonesByPatientId(patient.Id))!.Count == 2, "Retry after rollback creates phones once");

            await MustReject(() => service.SavePhones(another.Id, [additions[0]]));
            check((await service.GetPhonesByPatientId(patient.Id))!.Count == 2, "Cannot move phone ID to another patient");
            context.SetCurrentUser(2);
            check((await service.GetPhonesByPatientId(patient.Id))!.Count == 0 && await service.GetPhoneById(additions[0].Id) == null, "Another account cannot read telephone contacts");
            await MustReject(() => service.SavePhones(patient.Id, []));
            await service.DeletePhone(additions[0].Id);
            context.SetCurrentUser(1);
            check((await service.GetPhonesByPatientId(patient.Id))!.Count == 2, "Another account cannot save or delete telephone contacts");
            await service.SavePhones(patient.Id, []);
            check((await service.GetPhonesByPatientId(patient.Id))!.Count == 0, "Empty saved list deletes all patient phones");
            check(await connection.ExecuteScalarAsync<string>("PRAGMA integrity_check") == "ok", "Telephone integration leaves SQLite intact");
        }
        finally { await connection.CloseAsync(); }
    }

    private static async Task MustReject(Func<Task> action)
    {
        try { await action(); }
        catch (Exception e) when (e is SQLiteException or InvalidOperationException or ArgumentException) { return; }
        throw new InvalidOperationException("Expected telephone operation rejection.");
    }
}
