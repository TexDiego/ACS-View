using ACS_View.Domain.Entities;
using ACS_View.UseCases.DTOs;

namespace ACS_View.Domain.Interfaces
{
    public interface IPatientService
    {
        Task<Patient?> GetPatientById(int Id);
        Task<List<Patient>?> GetAllPatients();
        Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take);
        Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take, string? filterKey);
        Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take, PatientListFilterDto filter);
        Task<List<Patient>?> GetPatientsByCondition(int conditionId);
        Task<List<Patient>?> GetPatientsByHouseId(int houseId);
        Task<List<Patient>?> GetPatientsByFamilyAndHouseId(int familyId, int houseId);
        Task CreatePatient(Patient patient);
        Task UpdatePatient(Patient patient);
        Task DeletePatient(int Id);
    }
}
