using ACS_View.Domain.Entities;
namespace ACS_View.Application.Interfaces;
public interface IGlucoseMonitoringService
{
    Task<PatientGlucoseMonitoring?> GetAsync(int patientId);
    Task<List<PatientStripPickup>> GetPickupsAsync(int patientId);
    Task SaveAsync(int patientId, PatientGlucoseMonitoring monitoring);
    Task AddPickupAsync(int patientId, DateTime date, int quantity, string notes);
}
