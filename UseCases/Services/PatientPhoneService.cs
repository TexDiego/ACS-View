using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
namespace ACS_View.UseCases.Services;

public sealed class PatientPhoneService(IPatientPhoneRepository repository) : IPatientPhoneService
{
    public Task<List<Phone>?> GetPhonesByPatientId(int patientId) => repository.GetPhonesByPatientId(patientId);
    public Task<Phone?> GetPhoneById(int phoneId) => repository.GetPhoneById(phoneId);
    public Task CreatePhone(Phone phone) => repository.CreatePhone(phone);
    public Task UpdatePhone(Phone phone) => repository.UpdatePhone(phone);
    public Task DeletePhone(int phoneId) => repository.DeletePhone(phoneId);
    public Task SavePhones(int patientId, IReadOnlyList<Phone> phones) => repository.SavePhones(patientId, phones);
}
