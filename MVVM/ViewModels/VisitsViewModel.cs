using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public class VisitsViewModel
    {
        private readonly VisitsService _visitsService;

        public List<Visits> VisitsList { get; set; } = [];

        public ICommand RegisterVisitCommand { get; set; }

        public VisitsViewModel()
        {
            _visitsService = new VisitsService();

            RegisterVisitCommand = new Command<Visits>(RegisterVisit);
        }

        private async void RegisterVisit(Visits visit)
        {
            try
            {
                var newVisit = new Visits
                {
                    HouseId = visit.HouseId,
                    FamilyId = visit.FamilyId,
                    Date = visit.Date,
                    Description = visit.Description,
                    Address = visit.Address
                };
                await _visitsService.RegisterVisitAsync(visit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar visita: {ex.Message}");
            }
        }
    }
}
