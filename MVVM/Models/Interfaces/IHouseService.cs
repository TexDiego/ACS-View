namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IHouseService
    {
        Task<int> GetTotalCountAsync();
        Task<List<House>> GetAllHousesAsync();
        Task<House?> GetHouseByIdAsync(int id);
        Task<House?> GetHouseBySusAsync(string susNumber);
        Task SaveHouseAsync(House house);
        Task UpdateHouseAsync(House house);
        Task DeleteHouseAsync(int id);
        Task<int> GetMaxIdAsync();
    }
}