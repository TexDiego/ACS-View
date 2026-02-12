using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IRegisterManager
    {
        Task CreateAsync(HealthRecord record);
    }
}