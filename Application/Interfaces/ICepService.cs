using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface ICepService
{
    Task<House?> GetAddressByCepAsync(string cep);
}
