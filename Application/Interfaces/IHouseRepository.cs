using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IHouseRepository
{
    Task DeleteAsync(House house);
    Task<List<House>> GetAllAsync();
    Task<PagedResultDto<HouseListItemDto>> GetListAsync(string? search, int skip, int take);
    Task<House?> GetByIdAsync(int id);
    Task<House?> GetByPatientIdAsync(int id);
    Task<int> GetMaxIdAsync();
    Task<int> GetTotalCountAsync();
    Task InsertAsync(House house);
    Task UpdateAsync(House house);
}
