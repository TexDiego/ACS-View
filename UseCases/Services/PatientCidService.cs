using ACS_View.Domain.Entities.Health;
using ACS_View.Application.Interfaces;
using SQLite;

namespace ACS_View.UseCases.Services
{
    internal class PatientCidService(IDatabaseService db, ICurrentUserContext currentUserContext) : IPatientCidRepository
    {
        private readonly SQLiteAsyncConnection _connection = db.Connection;

        public async Task CreatePatientCID(PatientCID CID)
        {
            CID.UserId = currentUserContext.RequireCurrentUserId();
            await _connection.InsertAsync(CID);
        }

        public async Task UpdatePatientCID(PatientCID CID)
        {
            CID.UserId = currentUserContext.RequireCurrentUserId();
            await _connection.UpdateAsync(CID);
        }

        public async Task DeletePatientCID(int id)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            await _connection.ExecuteAsync("DELETE FROM PatientCID WHERE Id = ? AND UserId = ?", id, userId);
        }

        public async Task DeletePatientCIDByPatientId(int id)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            await _connection.Table<PatientCID>().Where(c => c.PatientId == id && c.UserId == userId).DeleteAsync();
        }

        public async Task<List<PatientCID>?> GetPatientCIDsByCIDId(int id)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            return await _connection.Table<PatientCID>().Where(c => c.CidId == id && c.UserId == userId).ToListAsync();
        }

        public async Task<List<PatientCID>?> GetPatientCIDsByPatientId(int id)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            return await _connection.Table<PatientCID>().Where(c => c.PatientId == id && c.UserId == userId).ToListAsync();
        }
    }
}
