using ACS_View.Domain.Entities.Health;
using ACS_View.Application.Interfaces;
using SQLite;

namespace ACS_View.Infrastructure.Data.SQLite
{
    public class SQLiteConditionsRepository(IDatabaseService _db, ICurrentUserContext currentUserContext) : ISQLiteConditionsRepository
    {
        private readonly SQLiteAsyncConnection _connection = _db.Connection;

        public async Task DeleteConditionAsync(int Id)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            await _connection.ExecuteAsync("DELETE FROM PatientConditions WHERE Id = ? AND UserId = ?", Id, userId);
        }

        public async Task DeleteConditionsByPatientIdAsync(int patientId)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            await _connection.Table<PatientConditions>().Where(c => c.PatientId == patientId && c.UserId == userId).DeleteAsync();
        }

        public async Task<List<PatientConditions>> GetAllConditionsAsync()
        {
            var userId = currentUserContext.RequireCurrentUserId();
            return await _connection.Table<PatientConditions>().Where(c => c.UserId == userId).ToListAsync();
        }

        public async Task<List<PatientConditions>> GetConditionsByPatientIdAsync(int patientId)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            return await _connection.Table<PatientConditions>().Where(c => c.PatientId == patientId && c.UserId == userId).ToListAsync();
        }

        public async Task InsertConditionAsync(PatientConditions condition)
        {
            condition.UserId = currentUserContext.RequireCurrentUserId();
            await _connection.InsertAsync(condition);
        }

        public async Task InsertConditionsAsync(List<PatientConditions> conditions)
        {
            var userId = currentUserContext.RequireCurrentUserId();
            foreach (var condition in conditions)
            {
                condition.UserId = userId;
            }

            await _connection.InsertAllAsync(conditions);
        }
    }
}
