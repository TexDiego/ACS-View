using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;
namespace ACS_View.Infrastructure.Data.Migrations;
internal static class PatientPersistence
{
    internal static void Save(SQLiteConnection c,Patient patient,int userId,bool syncLegacy=true)
    {
        if(patient.Id==0)
        {
            patient.UserId=userId;c.Insert(patient);RelationalStore.SyncPatient(c,patient);
            if(syncLegacy)KinshipSchema.SyncLegacyParents(c,patient,null);return;
        }
            var original=c.Table<Patient>().FirstOrDefault(p=>p.Id==patient.Id && p.UserId==userId);
            if(original==null) throw new InvalidOperationException("Paciente não pertence à conta atual.");
            if(original.RowRevision!=patient.RowRevision)
                throw new InvalidOperationException("O cadastro foi alterado desde a leitura. Recarregue antes de salvar.");
            patient.UserId=userId;
            if (original.SusNumber != patient.SusNumber) RelationalStore.IssueLegacySus(c, original);
            // sqlite-net materializes NULL scalars using model defaults. Write only fields
            // actually changed, so editing CNS/family cannot replace unknown dates or text.
            var changed=typeof(Patient).GetProperties().Where(p=>p.CanWrite && p.Name is not "Id" and not "UserId" and not "RowRevision"
                && !Attribute.IsDefined(p,typeof(IgnoreAttribute)) && !Equals(p.GetValue(original),p.GetValue(patient))).ToList();
            if(changed.Count>0)
            {
                var values=changed.Select(p=>p.GetValue(patient)!).ToList();values.Add(patient.Id);values.Add(userId);
                c.Execute($"UPDATE Patient SET {string.Join(",",changed.Select(p=>$"{SchemaMigrator.Quote(p.Name)}=?"))} WHERE Id=? AND UserId=?",values.ToArray());
            }
            RelationalStore.SyncPatient(c,patient);
            if(syncLegacy) KinshipSchema.SyncLegacyParents(c,patient,original);
            patient.RowRevision=c.ExecuteScalar<int>("SELECT RowRevision FROM Patient WHERE Id=? AND UserId=?",patient.Id,userId);
    }
}
