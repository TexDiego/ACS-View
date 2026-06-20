using ACS_View.Domain.Entities.Health;

namespace ACS_View.Domain.Interfaces
{
    public interface IPatientCidRepository
    {
        Task<List<PatientCID>?> GetPatientCIDsByPatientId(int id);
        Task<List<PatientCID>?> GetPatientCIDsByCIDId(int id);

        Task CreatePatientCID(PatientCID CID);
        Task UpdatePatientCID(PatientCID CID);
        Task DeletePatientCID(int id);
        Task DeletePatientCIDByPatientId(int id);
    }
}