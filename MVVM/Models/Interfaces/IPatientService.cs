namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface IPatientService
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