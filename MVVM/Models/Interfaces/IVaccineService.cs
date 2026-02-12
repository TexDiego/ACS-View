namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IVaccineService
    {
        Task<Vaccines> GetVaccinesBySusAsync(string sus);
        Task AdicionarVacinasAsync(Vaccines registro);
        Task AtualizarVacinasAsync(Vaccines registro);
    }
}
