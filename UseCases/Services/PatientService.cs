using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.UseCases.Services
{
    internal class PatientService(IPatientRepository repository) : IPatientService
    {
        public async Task DeletePatient(int Id)
        {
            await repository.DeleteAsync(Id);
            DataChangeTracker.MarkPatientsChanged();
        }

        public Task<List<Patient>?> GetAllPatients()
        {
            return repository.GetAllAsync();
        }

        public Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take)
        {
            return GetPatientListAsync(search, skip, take, new PatientListFilterDto());
        }

        public Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take, string? filterKey)
        {
            return GetPatientListAsync(search, skip, take, new PatientListFilterDto
            {
                FilterKey = string.IsNullOrWhiteSpace(filterKey) ? "ALL" : filterKey
            });
        }

        public Task<PagedResultDto<PatientListItemDto>> GetPatientListAsync(string? search, int skip, int take, PatientListFilterDto filter)
        {
            return repository.GetListAsync(search, skip, take, filter);
        }

        public Task<List<Patient>?> GetPatientsByCondition(int conditionId)
        {
            return repository.GetByConditionAsync(conditionId);
        }

        public Task<Patient?> GetPatientById(int Id)
        {
            return repository.GetByIdAsync(Id);
        }

        public async Task CreatePatient(Patient patient)
        {
            NormalizePatientNames(patient);
            await repository.InsertAsync(patient);
            DataChangeTracker.MarkPatientsChanged();
        }

        public async Task UpdatePatient(Patient patient)
        {
            NormalizePatientNames(patient);
            await repository.UpdateAsync(patient);
            DataChangeTracker.MarkPatientsChanged();
        }

        public Task<List<Patient>?> GetPatientsByHouseId(int houseId)
        {
            return repository.GetByHouseIdAsync(houseId);
        }

        public Task<List<Patient>?> GetPatientsByFamilyAndHouseId(int familyId, int houseId)
        {
            return repository.GetByFamilyAndHouseIdAsync(familyId, houseId);
        }

        private static void NormalizePatientNames(Patient patient)
        {
            patient.Name = PatientNameRules.NormalizeRequired(patient.Name, "Nome");
            patient.MotherName = PatientNameRules.NormalizeOptional(patient.MotherName, "Nome da mãe");
            patient.FatherName = PatientNameRules.NormalizeOptional(patient.FatherName, "Nome do pai");
            patient.SearchName = SearchTextNormalizer.Normalize(patient.Name);
            patient.SearchMotherName = SearchTextNormalizer.Normalize(patient.MotherName);
            patient.SearchFatherName = SearchTextNormalizer.Normalize(patient.FatherName);
        }
    }
}
