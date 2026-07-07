using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IPregnancyService
{
    Task<PatientPregnancy?> GetActiveOrLatestByPatientIdAsync(int patientId);
    Task<PregnancyDetailsDto?> GetDetailsByPatientIdAsync(int patientId);
    Task<PatientPregnancy> SaveAsync(PatientPregnancy pregnancy);
    Task SyncPregnancyConditionAsync(int patientId, bool isPregnant);
}
