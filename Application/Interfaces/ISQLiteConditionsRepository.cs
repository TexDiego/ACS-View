using ACS_View.Domain.Entities.Health;

namespace ACS_View.Application.Interfaces
{
    public interface ISQLiteConditionsRepository
    {
        Task InsertConditionsAsync(List<PatientConditions> conditions);
        Task InsertConditionAsync(PatientConditions condition);
        Task DeleteConditionAsync(int Id);
        Task DeleteConditionsByPatientIdAsync(int patientId);

        Task<List<PatientConditions>> GetAllConditionsAsync();
        Task<List<PatientConditions>> GetConditionsByPatientIdAsync(int patientId);
    }
}
