using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface IRegisterFactory
    {
        Patient CreateHealthRecord(AddRegisterViewModel vm);
        Vaccines CreateVaccines(AddRegisterViewModel vm);
    }
}