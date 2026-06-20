using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Interfaces;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class PatientCidService(IDatabaseService db) : IPatientCidRepository
    {
        private readonly SQLiteAsyncConnection _connection = db.Connection;

        public async Task CreatePatientCID(PatientCID CID)
        {
            await _connection.InsertAsync(CID);
        }

        public async Task UpdatePatientCID(PatientCID CID)
        {
            await _connection.UpdateAsync(CID);
        }

        public async Task DeletePatientCID(int id)
        {
            await _connection.DeleteAsync<PatientCID>(id);
        }

        public async Task DeletePatientCIDByPatientId(int id)
        {
            await _connection.Table<PatientCID>().Where(c => c.PatientId == id).DeleteAsync();
        }

        public async Task<List<PatientCID>?> GetPatientCIDsByCIDId(int id)
        {
            return await _connection.Table<PatientCID>().Where(c => c.CidId == id).ToListAsync();
        }

        public async Task<List<PatientCID>?> GetPatientCIDsByPatientId(int id)
        {
            return await _connection.Table<PatientCID>().Where(c => c.PatientId == id).ToListAsync();
        }
    }
}