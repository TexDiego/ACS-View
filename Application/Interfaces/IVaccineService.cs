using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces
{
    public interface IVaccineService
    {
        Task<PatientVaccineScheduleDto?> GetScheduleForPatientAsync(int patientId);
        Task SetDoseStatusAsync(int patientId, string doseKey, bool isApplied);
    }
}
