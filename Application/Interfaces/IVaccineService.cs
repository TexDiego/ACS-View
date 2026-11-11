using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces
{
    public interface IVaccineService
    {
        Task<PatientVaccineScheduleDto?> GetScheduleForPatientAsync(int patientId);
        Task ApplyDoseAsync(VaccineApplicationRequestDto request);
        Task ApplyOverdueDosesAsync(int patientId, int expectedUserId, IReadOnlyDictionary<string, DateTime> applicationDates);
        Task RemoveDoseApplicationAsync(int patientId, string doseKey);
    }
}
