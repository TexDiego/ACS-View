using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class VisitsViewModel : BaseViewModel
    {
        private readonly IVisitsService _visitsService = App.ServiceProvider.GetRequiredService<IVisitsService>();

        [ObservableProperty] private List<Visits> visitsList = [];


        public ICommand RegisterVisitCommand => new Command<Visits>(RegisterVisit);

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
