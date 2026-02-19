using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface IRegisterManager
    {
        Task CreateAsync(Patient record);
    }
}