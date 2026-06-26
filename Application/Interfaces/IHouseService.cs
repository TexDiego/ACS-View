using ACS_View.Domain.Entities;
using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces
{
    public interface IHouseService
    {
        Task<int> GetTotalCountAsync();
        Task<List<House>> GetAllHousesAsync();
        Task<PagedResultDto<HouseListItemDto>> GetHouseListAsync(string? search, int skip, int take, string? filterKey = null);
        Task<House?> GetHouseByIdAsync(int id);
        Task<House?> GetHouseByPatientIdAsync(int id);
        Task SaveHouseAsync(House house);
        Task UpdateHouseAsync(House house);
        Task DeleteHouseAsync(int id);
        Task<int> GetMaxIdAsync();
    }
}
