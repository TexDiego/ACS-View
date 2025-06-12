using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public class VisitsViewModel
    {
        private readonly DatabaseService _databaseService;
        private VisitsService _visitsService;

        public List<Visits> VisitsList { get; set; } = new List<Visits>();

        public ICommand RegisterVisitCommand { get; set; }

        public VisitsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "O serviço de banco de dados não pode ser nulo.");
            _visitsService = new VisitsService(_databaseService);

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
