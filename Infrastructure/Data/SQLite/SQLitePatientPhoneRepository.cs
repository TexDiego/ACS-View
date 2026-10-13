using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.Migrations;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLitePatientPhoneRepository(IDatabaseService database, ICurrentUserContext context,
    IRelationalDataService relational) : IPatientPhoneRepository
{
    public async Task<List<Phone>?> GetPhonesByPatientId(int patientId) =>
        (await relational.GetPhonesAsync(patientId)).OrderBy(p => p.Id).Select(ToPhone).ToList();

    public async Task<Phone?> GetPhoneById(int phoneId)
    {
        var user = context.RequireCurrentUserId();
        var row = await database.Connection.Table<PatientPhone>().Where(p => p.Id == phoneId && p.UserId == user).FirstOrDefaultAsync();
        return row is null ? null : ToPhone(row);
    }

    public async Task CreatePhone(Phone phone)
    {
        if (phone.Id != 0) throw new ArgumentException("O telefone já possui um cadastro.");
        var row = new PatientPhone { PatientId = phone.PatientId, Number = phone.Number, IsPrimary = phone.IsPrimary };
        await relational.SavePhoneAsync(row);
        phone.Id = row.Id;
    }

    public async Task UpdatePhone(Phone phone)
    {
        var user = context.RequireCurrentUserId();
        var row = await database.Connection.Table<PatientPhone>().Where(p => p.Id == phone.Id && p.UserId == user && p.PatientId == phone.PatientId).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Telefone não pertence ao paciente atual.");
        row.Number = phone.Number;
        row.IsPrimary = phone.IsPrimary;
        await relational.SavePhoneAsync(row);
    }

    public Task DeletePhone(int phoneId) => relational.DeletePhoneAsync(phoneId);

    public async Task SavePhones(int patientId, IReadOnlyList<Phone> phones)
    {
        var user = context.RequireCurrentUserId();
        var items = phones.Select(p => new Phone { Id = p.Id, PatientId = patientId, Number = p.Number.Trim(), IsPrimary = p.IsPrimary }).ToList();
        if (items.Any(p => Phone.Digits(p.Number).Length == 0)) throw new ArgumentException("Informe um número de telefone.");
        if (items.GroupBy(p => Phone.Digits(p.Number)).Any(g => g.Count() > 1)) throw new ArgumentException("Este telefone já está na lista.");
        if (items.Count(p => p.IsPrimary) > 1) throw new ArgumentException("Selecione somente um telefone principal.");
        if (items.Where(p => p.Id != 0).GroupBy(p => p.Id).Any(g => g.Count() > 1)) throw new ArgumentException("Telefone repetido na lista.");
        await database.Connection.RunInTransactionAsync(c =>
        {
            if (c.Table<Patient>().FirstOrDefault(p => p.Id == patientId && p.UserId == user) is null)
                throw new InvalidOperationException("Paciente não pertence à conta atual.");
            var existing = c.Table<PatientPhone>().Where(p => p.PatientId == patientId && p.UserId == user).ToList();
            if (items.Any(p => p.Id != 0 && existing.All(e => e.Id != p.Id)))
                throw new InvalidOperationException("Telefone não pertence ao paciente atual.");
            foreach (var row in existing.Where(e => items.All(p => p.Id != e.Id))) c.Delete(row);
            // Clear unique values first so swapping numbers or the primary flag is safe.
            c.Execute("UPDATE PatientPhone SET IsPrimary=0, NormalizedNumber=NULL WHERE PatientId=? AND UserId=?", patientId, user);
            foreach (var item in items)
            {
                var row = existing.FirstOrDefault(p => p.Id == item.Id) ?? new PatientPhone { PatientId = patientId, UserId = user };
                row.Number = item.Number;
                var digits = Phone.Digits(item.Number);
                row.NormalizedNumber = digits.Length is >= 8 and <= 15 ? digits : null;
                row.IsPrimary = item.IsPrimary;
                if (row.Id == 0) c.Insert(row); else c.Update(row);
                item.Id = row.Id;
                if (row.NormalizedNumber is null) RelationalStore.Issue(c, user, "PatientPhone", row.Id, "IncompletePhone", row.Number);
            }
        });
        // Publish generated IDs only after commit, allowing retries after a rollback.
        for (var i = 0; i < phones.Count; i++)
        {
            phones[i].Id = items[i].Id;
            phones[i].PatientId = patientId;
        }
    }
    private static Phone ToPhone(PatientPhone row) => new() { Id = row.Id, PatientId = row.PatientId, Number = row.Number, IsPrimary = row.IsPrimary };
}
