using ACS_View.Application.DTOs;
using ACS_View.Application.Health;
namespace ACS_View.Application.Interfaces;
public interface IPatientHealthDataService
{
    Task<ACS_View.Domain.Entities.PatientInfectiousFollowUp?> GetInfectiousAsync(PatientHealthSession session, string conditionCode);
    Task<ClinicalHealthData> GetClinicalAsync(PatientHealthSession session);
    Task<DiabetesHealthData> GetDiabetesAsync(PatientHealthSession session);
    Task<PatientVaccineScheduleDto?> GetVaccinationAsync(PatientHealthSession session);
    Task<VisitsHealthData> GetVisitsAsync(PatientHealthSession session);
}
