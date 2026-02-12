using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IRegisterFactory
    {
        HealthRecord CreateHealthRecord(AddRegisterViewModel vm);
        Vaccines CreateVaccines(AddRegisterViewModel vm);
    }
}