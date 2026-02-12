using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;

namespace ACS_View.MVVM.Models.Services
{
    public class PersonsInfoService : IPersonsInfoService
    {
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();

        public async Task<string> GetEnderecoAsync(string susNumber)
        {
            var house = await _houseService.GetHouseBySusAsync(susNumber);
            if (house == null) return "Endereço não encontrado";

            var rua = house.Rua ?? "";
            var numeroRua = house.NumeroCasa ?? "";
            return $"{rua}, {numeroRua}";
        }

        public async Task<string> GetComplementoAsync(string susNumber)
        {
            var house = await _houseService.GetHouseBySusAsync(susNumber);
            return house?.PossuiComplemento == true ? house.Complemento : "Sem complemento";
        }

        public async Task<bool> TemComplementoAsync(string susNumber)
        {
            var house = await _houseService.GetHouseBySusAsync(susNumber);
            return house?.PossuiComplemento ?? false;
        }
    }
}