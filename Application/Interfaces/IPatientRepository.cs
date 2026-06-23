using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IPatientRepository
{
    Task DeleteAsync(int id);
    Task<List<Patient>?> GetAllAsync();
    Task<PagedResultDto<PatientListItemDto>> GetListAsync(string? search, int skip, int take, PatientListFilterDto filter);
    Task<List<Patient>?> GetByConditionAsync(int conditionId);
    Task<Patient?> GetByIdAsync(int id);
    Task InsertAsync(Patient patient);
    Task UpdateAsync(Patient patient);
    Task<List<Patient>?> GetByHouseIdAsync(int houseId);
    Task<List<Patient>?> GetByFamilyAndHouseIdAsync(int familyId, int houseId);
}
