namespace ACS_View.Application.Interfaces
{
    public interface IPatientSusService
    {
        Task<List<ACS_View.Domain.Entities.PatientSusNumber>> GetNumbersAsync(int patientId);
        Task SaveNumbersAsync(int patientId, IReadOnlyList<ACS_View.Domain.Entities.PatientSusNumber> numbers);
        Task<string> GetSusByIdAsync(int susId);
        Task<List<string>> GetAllPatientSusAsync(int patientId);
        Task<string> GetMainSusAsync(int patientId);
        Task CreateSusAsync(int patientId, string sus);
        Task UpdateSusAsync(int susId, string sus);
        Task DeleteSusAsync(int susId);
    }
    public interface IPatientSusRepository : IPatientSusService { }
}
