using ACS_View.MVVM.Models.HealthConditions;

namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface IPatientConditionsService
    {
        Task CreateCondition(PatientCondition condition);
        Task<PatientCondition> GetCondition(int patientId);
        Task UpdateCondition(PatientCondition condition);
        Task DeleteCondition(PatientCondition condition);
    }
}