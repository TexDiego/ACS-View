using ACS_View.MVVM.Models.Interfaces;
using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    internal class PatientService : IPatientService
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly IDatabaseService _db;

        public PatientService(IDatabaseService db)
        {
            _db = db;
            _connection = _db.Connection;
        }

        public async Task DeletePatient(int Id)
        {
            await _connection.DeleteAsync<Patient>(Id);
        }

        public Task<List<Patient>?> GetAllPatients()
        {
            return _connection.Table<Patient>()
                              .OrderBy(n => n.Name)
                              .ToListAsync();
        }

        public async Task<List<Patient>?> GetPatientsByCondition(int conditionId)
        {
            if (conditionId <= 0) return await _connection.Table<Patient>().OrderBy(p => p.Name).ToListAsync();

            return await _connection.QueryAsync<Patient>(@"
                SELECT p.*
                FROM Patient p
                INNER JOIN PatientCondition pc ON pc.PatientId = p.Id
                WHERE pc.ConditionId = ?
                ORDER BY p.Name",
                conditionId);
        }

        public Task<Patient?> GetPatientById(int Id)
        {
            return _connection.Table<Patient>()
                              .FirstOrDefaultAsync(p => p.Id == Id);
        }

        public async Task CreatePatient(Patient patient)
        {
            await _connection.InsertAsync(patient);
        }

        public async Task UpdatePatient(Patient patient)
        {
            await _connection.UpdateAsync(patient);
        }

        public async Task<List<Patient>?> GetPatientsByHouseId(int houseId)
        {
            return await _connection.Table<Patient>().Where(p => p.HouseId == houseId).ToListAsync();
        }
    }
}