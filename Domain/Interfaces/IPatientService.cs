using ACS_View.Domain.Entities;

namespace ACS_View.Domain.Interfaces
{
    public interface IPatientService
    {
        Task<Patient?> GetPatientById(int Id);
        Task<List<Patient>?> GetAllPatients();
        Task<List<Patient>?> GetPatientsByCondition(int conditionId);
        Task<List<Patient>?> GetPatientsByHouseId(int houseId);
        Task CreatePatient(Patient patient);
        Task UpdatePatient(Patient patient);
        Task DeletePatient(int Id);
    }
}