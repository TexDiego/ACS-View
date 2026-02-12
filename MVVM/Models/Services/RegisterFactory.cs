using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Models.Services
{
    public class RegisterFactory : IRegisterFactory
    {
        public HealthRecord CreateHealthRecord(AddRegisterViewModel vm)
        {
            return vm.Record;
        }

        public Vaccines CreateVaccines(AddRegisterViewModel vm)
        {
            return new Vaccines
            {
                SusNumber = vm.Record.SusNumber,
                BirthDate = vm.Record.BirthDate,
                IsPregnant = vm.Record.IsPregnant
                // demais campos podem iniciar false por padrão no banco
            };
        }
    }
}