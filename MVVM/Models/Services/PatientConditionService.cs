using ACS_View.MVVM.Models.HealthConditions;
using ACS_View.MVVM.Models.Interfaces;
using SQLite;

namespace ACS_View.MVVM.Models.Services
{
    internal class PatientConditionService(IDatabaseService _db) : IPatientConditionsService
    {
        private readonly SQLiteAsyncConnection _connection = _db.Connection;

        public async Task CreateCondition(PatientCondition condition)
        {
            await _connection.InsertAsync(condition);
        }

        public async Task DeleteCondition(PatientCondition condition)
        {
            await _connection.DeleteAsync(condition);
        }

        public async Task<PatientCondition> GetCondition(int patientId)
        {
            return await _connection.Table<PatientCondition>().Where(c => c.PatientId == patientId).FirstOrDefaultAsync();
        }

        public async Task UpdateCondition(PatientCondition condition)
        {
            await _connection.UpdateAsync(condition);
        }
    }
}