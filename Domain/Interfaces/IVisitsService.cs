using ACS_View.Domain.Entities;

namespace ACS_View.Domain.Interfaces
{
    internal interface IVisitsService
    {
        Task<List<Visits>> GetAllVisitsAsync();
        Task RegisterVisitAsync(Visits visit);
        Task DeleteVisitAsync(int id);
    }
}