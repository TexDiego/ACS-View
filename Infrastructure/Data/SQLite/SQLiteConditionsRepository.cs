using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Interfaces;
using SQLite;

namespace ACS_View.Infrastructure.Data.SQLite
{
    public class SQLiteConditionsRepository(IDatabaseService _db) : ISQLiteConditionsRepository
    {
        private readonly SQLiteAsyncConnection _connection = _db.Connection;

        public async Task DeleteConditionAsync(int Id)
        {
            await _connection.DeleteAsync<PatientConditions>(Id);
        }

        public async Task DeleteConditionsByPatientIdAsync(int patientId)
        {
            await _connection.Table<PatientConditions>().Where(c => c.PatientId == patientId).DeleteAsync();
        }

        public async Task<List<PatientConditions>> GetAllConditionsAsync()
        {
            return await _connection.Table<PatientConditions>().ToListAsync();
        }

        public async Task<List<PatientConditions>> GetConditionsByPatientIdAsync(int patientId)
        {
            return await _connection.Table<PatientConditions>().Where(c => c.PatientId == patientId).ToListAsync();
        }

        public async Task InsertConditionAsync(PatientConditions condition)
        {
            await _connection.InsertAsync(condition);
        }

        public async Task InsertConditionsAsync(List<PatientConditions> conditions)
        {
            await _connection.InsertAllAsync(conditions);
        }
    }
}
