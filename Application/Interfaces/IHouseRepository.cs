using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IHouseRepository
{
    Task<List<House>> SearchTerritorialAsync(string street);
    Task<List<House>> GetTerritorialSelectionAsync(IReadOnlyCollection<int> ids);
    Task<int> ApplyTerritoryAsync(IReadOnlyCollection<House> reviewed, string sector, string block, bool onlyUnclassified);
    Task DeleteAsync(House house);
    Task<List<House>> GetAllAsync(string? search = null);
    Task<PagedResultDto<HouseListItemDto>> GetListAsync(string? search, int skip, int take, string? filterKey = null, HouseNumberFilterDto? numberFilter = null);
    Task<House?> GetByIdAsync(int id);
    Task<House?> GetByPatientIdAsync(int id);
    Task<int> GetMaxIdAsync();
    Task<int> GetTotalCountAsync();
    Task InsertAsync(House house);
    Task UpdateAsync(House house);
}
