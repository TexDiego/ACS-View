using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;

namespace ACS_View.MVVM.Models.Services
{
    public class PersonsInfoService : IPersonsInfoService
    {
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();

        public async Task<string> GetEnderecoAsync(int id)
        {
            var house = await _houseService.GetHouseByPatientIdAsync(id);
            if (house == null) return "Endereço não encontrado";

            var rua = house.Rua ?? "";
            var numeroRua = house.NumeroCasa ?? "";
            return $"{rua}, {numeroRua}";
        }

        public async Task<string?> GetComplementoAsync(int id)
        {
            var house = await _houseService.GetHouseByPatientIdAsync(id);
            return house?.Complemento?? null;
        }
    }
}