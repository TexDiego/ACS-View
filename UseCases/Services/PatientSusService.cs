using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
namespace ACS_View.UseCases.Services;

public sealed class PatientSusService(IPatientSusRepository repository) : IPatientSusService
{
    public Task<List<PatientSusNumber>> GetNumbersAsync(int patientId) => repository.GetNumbersAsync(patientId);
    public Task SaveNumbersAsync(int patientId, IReadOnlyList<PatientSusNumber> numbers) => repository.SaveNumbersAsync(patientId, numbers);
    public Task<string> GetSusByIdAsync(int id) => repository.GetSusByIdAsync(id);
    public Task<List<string>> GetAllPatientSusAsync(int patientId) => repository.GetAllPatientSusAsync(patientId);
    public Task<string> GetMainSusAsync(int patientId) => repository.GetMainSusAsync(patientId);
    public Task CreateSusAsync(int patientId, string sus) => repository.CreateSusAsync(patientId, sus);
    public Task UpdateSusAsync(int id, string sus) => repository.UpdateSusAsync(id, sus);
    public Task DeleteSusAsync(int id) => repository.DeleteSusAsync(id);
}
