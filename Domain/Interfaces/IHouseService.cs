using ACS_View.Domain.Entities;
using ACS_View.UseCases.DTOs;

namespace ACS_View.Domain.Interfaces
{
    public interface IHouseService
    {
        Task<int> GetTotalCountAsync();
        Task<List<House>> GetAllHousesAsync();
        Task<PagedResultDto<HouseListItemDto>> GetHouseListAsync(string? search, int skip, int take);
        Task<House?> GetHouseByIdAsync(int id);
        Task<House?> GetHouseByPatientIdAsync(int id);
        Task SaveHouseAsync(House house);
        Task UpdateHouseAsync(House house);
        Task DeleteHouseAsync(int id);
        Task<int> GetMaxIdAsync();
    }
}
