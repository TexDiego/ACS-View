using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Infrastructure.Data.Migrations;

namespace ACS_View.Infrastructure.Data.SQLite;

internal sealed class SQLitePatientSusRepository(IDatabaseService database, ICurrentUserContext context) : IPatientSusRepository
{
    public async Task<List<PatientSusNumber>> GetNumbersAsync(int patientId)
    {
        var user = context.RequireCurrentUserId();
        return (await database.Connection.Table<PatientSusNumber>().Where(n => n.PatientId == patientId && n.UserId == user).ToListAsync())
            .OrderByDescending(n => n.IsPrimary).ThenBy(n => n.OriginalOrder).ToList();
    }

    public async Task<string> GetSusByIdAsync(int id) => (await FindAsync(id))?.Number ?? string.Empty;
    public async Task<List<string>> GetAllPatientSusAsync(int patientId) => (await GetNumbersAsync(patientId)).Select(n => n.Number).ToList();
    public async Task<string> GetMainSusAsync(int patientId) => (await GetNumbersAsync(patientId)).FirstOrDefault(n => n.IsPrimary)?.Number ?? string.Empty;

    private Task<PatientSusNumber?> FindAsync(int id)
    {
        var user = context.RequireCurrentUserId();
        return database.Connection.Table<PatientSusNumber>().FirstOrDefaultAsync(n => n.Id == id && n.UserId == user);
    }

    public async Task CreateSusAsync(int patientId, string sus)
    {
        var rows = await GetNumbersAsync(patientId);
        rows.Add(new PatientSusNumber { Number = sus, IsPrimary = rows.Count == 0 });
        await SaveNumbersAsync(patientId, rows);
    }
    public async Task UpdateSusAsync(int id, string sus)
    {
        var row = await FindAsync(id) ?? throw new InvalidOperationException("Número SUS não pertence à conta atual.");
        var rows = await GetNumbersAsync(row.PatientId);
        rows.Single(n => n.Id == id).Number = sus;
        await SaveNumbersAsync(row.PatientId, rows);
    }
    public async Task DeleteSusAsync(int id)
    {
        var row = await FindAsync(id);
        if (row is null) return;
        var rows = await GetNumbersAsync(row.PatientId);
        rows.RemoveAll(n => n.Id == id);
        if (rows.Count > 0 && rows.All(n => !n.IsPrimary)) rows[0].IsPrimary = true;
        await SaveNumbersAsync(row.PatientId, rows);
    }

    public async Task SaveNumbersAsync(int patientId, IReadOnlyList<PatientSusNumber> numbers)
    {
        var user = context.RequireCurrentUserId();
        var items = numbers.Select(n => new PatientSusNumber { Id = n.Id, PatientId = patientId, UserId = user,
            Number = n.Number.Trim(), IsPrimary = n.IsPrimary }).ToList();
        if (items.Count(n => n.IsPrimary) > 1 || items.Where(n => n.Id != 0).GroupBy(n => n.Id).Any(g => g.Count() > 1))
            throw new ArgumentException("Selecione somente um SUS principal e não repita cadastros.");
        if (items.Count > 0 && items.All(n => !n.IsPrimary)) items[0].IsPrimary = true;
        await database.Connection.RunInTransactionAsync(c =>
        {
            var patient = c.Table<Patient>().FirstOrDefault(p => p.Id == patientId && p.UserId == user)
                ?? throw new InvalidOperationException("Paciente não pertence à conta atual.");
            var existing = c.Table<PatientSusNumber>().Where(n => n.PatientId == patientId && n.UserId == user).ToList();
            foreach (var item in items)
            {
                var original = existing.FirstOrDefault(n => n.Id == item.Id);
                if (item.Id != 0 && original is null) throw new InvalidOperationException("SUS não pertence ao paciente atual.");
                var parsed = SusNumberSet.Parse(item.Number);
                if (parsed.Count != 1) throw new ArgumentException("Informe um número SUS por cadastro.");
                item.Number = parsed[0];
                // Incomplete legacy evidence may remain untouched, but new/edited numbers need 15 digits.
                if (item.Number.Length != 15 && original?.Number != item.Number)
                    throw new ArgumentException("O número SUS deve ter 15 dígitos.");
                if (c.Table<PatientSusNumber>().Any(n => n.UserId == user && n.PatientId != patientId && n.Number == item.Number)
                    && original?.Number != item.Number)
                    throw new InvalidOperationException("Número SUS já pertence a outro paciente.");
            }
            if (items.GroupBy(n => n.Number).Any(g => g.Count() > 1)) throw new ArgumentException("Número SUS repetido.");
            RelationalStore.IssueLegacySus(c, patient);
            var ordered = items.OrderByDescending(n => n.IsPrimary).ToList();
            foreach (var row in existing.Where(n => items.All(i => i.Id != n.Id))) c.Delete(row);
            c.Execute("UPDATE PatientSusNumber SET IsPrimary=0, Number='__pending_sus_' || Id WHERE PatientId=? AND UserId=?", patientId, user);
            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].OriginalOrder = i;
                if (ordered[i].Id == 0) c.Insert(ordered[i]); else c.Update(ordered[i]);
            }
            // Keep old consumers compatible; archive raw evidence before any explicit replacement.
            c.Execute("UPDATE Patient SET SusNumber=? WHERE Id=? AND UserId=?", SusNumberSet.Join(ordered.Select(n => n.Number)), patientId, user);
        });
        for (var i = 0; i < items.Count; i++)
        {
            numbers[i].Id = items[i].Id;
            numbers[i].UserId = user;
            numbers[i].PatientId = patientId;
            numbers[i].Number = items[i].Number;
            numbers[i].IsPrimary = items[i].IsPrimary;
            numbers[i].OriginalOrder = items[i].OriginalOrder;
        }
    }
}
