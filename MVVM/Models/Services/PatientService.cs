using ACS_View.MVVM.Models.HealthConditions;
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

        public Task<List<Patient>> GetAllPatients()
        {
            return _connection.Table<Patient>()
                              .OrderBy(n => n.Name)
                              .ToListAsync();
        }

        public Task<List<Patient>> GetPatientsByCondition(int conditionId)
        {
            var patients = new List<Patient>();

            var conditions = _connection.Table<PatientCondition>().Where(i => i.Id == conditionId).ToListAsync();

            if (conditionId > 0)
                return _connection.Table<Patient>()
                                  .Where(i => i.Id == conditions.Id)
                                  .OrderBy(i => i.Name)
                                  .ToListAsync();

            else return _connection.Table<Patient>().OrderBy(i => i.Name).ToListAsync();
        }

        public Task<Patient> GetPatientById(int Id)
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
    }
}