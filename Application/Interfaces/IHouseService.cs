using ACS_View.Domain.Entities;
using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces
{
    public interface IHouseService
    {
        Task<List<House>> GetTerritorialHousesAsync(string? search = null);
        Task<List<House>> SearchTerritorialAsync(string street, int? start, int? end, ACS_View.Domain.ValueObjects.HouseParity parity);
        Task<List<House>> GetTerritorialSelectionAsync(IReadOnlyCollection<int> ids);
        Task<int> ApplyTerritoryAsync(IReadOnlyCollection<House> reviewed, string sector, string block, bool onlyUnclassified);
        Task<int> GetTotalCountAsync();
        Task<List<House>> GetAllHousesAsync();
        Task<PagedResultDto<HouseListItemDto>> GetHouseListAsync(string? search, int skip, int take, string? filterKey = null, HouseNumberFilterDto? numberFilter = null);
        Task<House?> GetHouseByIdAsync(int id);
        Task<House?> GetHouseByPatientIdAsync(int id);
        Task SaveHouseAsync(House house);
        Task UpdateHouseAsync(House house);
        Task DeleteHouseAsync(int id);
        Task<int> GetMaxIdAsync();
    }
}
