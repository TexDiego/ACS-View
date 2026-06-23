using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using System.Text.Json;

namespace ACS_View.Infrastructure.Services;

internal sealed class ViaCepService(HttpClient httpClient) : ICepService
{
    public async Task<House?> GetAddressByCepAsync(string cep)
    {
        using var response = await httpClient.GetAsync($"ws/{cep}/json/");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<House>(stream);
    }
}
