using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces
{
    public interface IVisitsService
    {
        Task<List<Visits>> GetAllVisitsAsync();
        Task RegisterVisitAsync(Visits visit);
        Task DeleteVisitAsync(int id);
    }
}