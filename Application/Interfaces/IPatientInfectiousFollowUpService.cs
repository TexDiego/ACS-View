using ACS_View.Domain.Entities;
namespace ACS_View.Application.Interfaces;
public interface IPatientInfectiousFollowUpService
{
    Task<PatientInfectiousFollowUp?> GetAsync(int patientId, string conditionCode);
    Task SaveAsync(int patientId, PatientInfectiousFollowUp value);
}
