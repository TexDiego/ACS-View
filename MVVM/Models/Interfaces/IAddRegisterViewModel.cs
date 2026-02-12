namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IAddRegisterViewModel
    {
        bool IsLoading { get; set; }

        Task SalvarAsync();
    }
}