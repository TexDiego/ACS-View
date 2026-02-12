namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IFamilyService
    {
        Task<List<Family>> GetAllFamiliesAsync();
        Task<Family> GetFamilyById(int id);
        Task<List<HealthRecord>> GetPersonOfFamilyById(int houseId, int familyId);
        Task<int> SaveFamilyAsync(Family family);
        Task<int> DeleteFamilyAsync(int id);
        Task UpdateFamilyAsync(Family family);
        Task<int> GetMaxIdAsync(int houseId);
    }
}
