using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.Infrastructure.Services;

internal sealed class CachedCepService(
    IDatabaseService databaseService,
    ViaCepService remoteCepService) : ICepService
{
    private readonly SQLiteAsyncConnection connection = databaseService.Connection;

    public async Task<House?> GetAddressByCepAsync(string cep)
    {
        var normalizedCep = CepNumberRules.Normalize(cep);
        if (!CepNumberRules.IsValid(normalizedCep))
        {
            return null;
        }

        var cachedAddress = await connection.Table<CepAddressCache>()
            .FirstOrDefaultAsync(address => address.Cep == normalizedCep);

        if (cachedAddress is not null && !IsSuspiciousCity(cachedAddress.Cidade))
        {
            return MapToHouse(cachedAddress);
        }

        var remoteAddress = await remoteCepService.GetAddressByCepAsync(normalizedCep);
        if (!HasAddressData(remoteAddress))
        {
            return remoteAddress;
        }

        remoteAddress!.CEP = normalizedCep;
        await SaveAsync(remoteAddress);
        return remoteAddress;
    }

    private Task SaveAsync(House house)
    {
        var cache = new CepAddressCache
        {
            Cep = CepNumberRules.Normalize(house.CEP),
            Rua = house.Rua?.Trim() ?? string.Empty,
            TipoLogradouro = house.TipoLogradouro?.Trim() ?? string.Empty,
            Bairro = house.Bairro?.Trim() ?? string.Empty,
            Cidade = house.Cidade?.Trim() ?? string.Empty,
            Estado = house.Estado?.Trim() ?? string.Empty,
            Pais = string.IsNullOrWhiteSpace(house.Pais) ? "Brasil" : house.Pais.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        return connection.InsertOrReplaceAsync(cache);
    }

    private static House MapToHouse(CepAddressCache cache)
    {
        return new House
        {
            CEP = cache.Cep,
            Rua = cache.Rua,
            TipoLogradouro = cache.TipoLogradouro,
            Bairro = cache.Bairro,
            Cidade = cache.Cidade,
            Estado = cache.Estado,
            Pais = string.IsNullOrWhiteSpace(cache.Pais) ? "Brasil" : cache.Pais
        };
    }

    private static bool HasAddressData(House? house)
    {
        return house is not null &&
               (!string.IsNullOrWhiteSpace(house.Rua) ||
                !string.IsNullOrWhiteSpace(house.Bairro) ||
                !string.IsNullOrWhiteSpace(house.Cidade) ||
                !string.IsNullOrWhiteSpace(house.Estado));
    }

    private static bool IsSuspiciousCity(string? value)
    {
        var normalized = value?.Trim();
        return !string.IsNullOrWhiteSpace(normalized) && normalized.All(char.IsDigit);
    }
}
