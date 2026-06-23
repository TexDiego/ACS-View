using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces
{
    public interface IVaccineService
    {
        Task<Vaccines> GetVaccinesByIdAsync(int id);
        Task AdicionarVacinasAsync(Vaccines registro);
        Task AtualizarVacinasAsync(Vaccines registro);
    }
}
