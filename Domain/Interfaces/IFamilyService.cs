using ACS_View.Domain.Entities;

namespace ACS_View.Domain.Interfaces
{
    internal interface IFamilyService
    {
        Task<List<Family>> GetAllFamiliesAsync();
        Task<Family> GetFamilyById(int id);
        Task<List<Patient>> GetPersonOfFamilyById(int houseId, int familyId);
        Task<int> SaveFamilyAsync(Family family);
        Task<int> DeleteFamilyAsync(int id);
        Task UpdateFamilyAsync(Family family);
        Task<int> GetMaxIdAsync(int houseId);
    }
}
