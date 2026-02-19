namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IVaccineService
    {
        Task<Vaccines> GetVaccinesByIdAsync(int id);
        Task AdicionarVacinasAsync(Vaccines registro);
        Task AtualizarVacinasAsync(Vaccines registro);
    }
}
