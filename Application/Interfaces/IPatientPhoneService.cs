using ACS_View.Domain.ValueObjects;

namespace ACS_View.Application.Interfaces
{
    public interface IPatientPhoneService
    {
        Task<List<Phone>?> GetPhonesByPatientId(int patientId);
        Task<Phone?> GetPhoneById(int phoneId);
        Task CreatePhone(Phone phone);
        Task UpdatePhone(Phone phone);
        Task DeletePhone(int phoneId);
        Task SavePhones(int patientId, IReadOnlyList<Phone> phones);
    }
    public interface IPatientPhoneRepository : IPatientPhoneService { }
}
