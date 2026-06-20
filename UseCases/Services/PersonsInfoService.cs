using ACS_View.Domain.Interfaces;

namespace ACS_View.UseCases.Services
{
    public class PersonsInfoService(IHouseService _houseService) : IPersonsInfoService
    {
        public async Task<string> GetEnderecoAsync(int id)
        {
            var addressInfo = await GetAddressInfoAsync(id);
            return addressInfo.Endereco;
        }

        public async Task<string?> GetComplementoAsync(int id)
        {
            var addressInfo = await GetAddressInfoAsync(id);
            return addressInfo.Complemento;
        }

        public async Task<(string Endereco, string Complemento)> GetAddressInfoAsync(int id)
        {
            var house = await _houseService.GetHouseByPatientIdAsync(id);
            if (house == null) return ("Endereço não encontrado", string.Empty);

            var rua = house.Rua ?? "";
            var numeroRua = house.NumeroCasa ?? "";
            return ($"{rua}, {numeroRua}", house.Complemento ?? string.Empty);
        }
    }
}
