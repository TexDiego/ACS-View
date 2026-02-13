namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface IVisitsService
    {
        Task<List<Visits>> GetAllVisitsAsync();
        Task RegisterVisitAsync(Visits visit);
        Task DeleteVisitAsync(int id);
    }
}