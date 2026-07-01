using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IPatientInsulinDependencyRepository
{
    Task<PatientInsulinDependency?> GetByPatientIdAsync(int patientId);
    Task UpsertAsync(int patientId);
    Task DeleteByPatientIdAsync(int patientId);
}
