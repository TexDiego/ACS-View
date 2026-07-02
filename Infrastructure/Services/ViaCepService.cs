using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using System.Text.Json;

namespace ACS_View.Infrastructure.Services;

internal sealed class ViaCepService(HttpClient httpClient) : ICepService
{
    public async Task<House?> GetAddressByCepAsync(string cep)
    {
        using var response = await httpClient.GetAsync($"ws/{cep}/json/");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var house = await JsonSerializer.DeserializeAsync<House>(stream);
        if (house is null)
        {
            return null;
        }

        var streetParts = StreetAddressParser.SplitStreetType(house.Rua);
        house.TipoLogradouro = streetParts.StreetType;
        house.Rua = streetParts.StreetName;

        return house;
    }
}
