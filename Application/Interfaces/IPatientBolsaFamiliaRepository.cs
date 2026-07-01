using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.Application.Interfaces;

public interface IPatientBolsaFamiliaRepository
{
    Task<PatientBolsaFamilia?> GetByPatientIdAsync(int patientId);
    Task<List<BolsaFamiliaGroup>> GetGroupsAsync();
    Task UpsertAsync(PatientBolsaFamilia benefit);
    Task DeleteByPatientIdAsync(int patientId);
}
