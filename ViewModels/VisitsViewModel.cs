using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class VisitsViewModel(IVisitsService _visitsService) : BaseViewModel
    {
        [ObservableProperty] private List<Visits> visitsList = [];

        public ICommand RegisterVisitCommand => new Command<Visits>(RegisterVisit);

        private async void RegisterVisit(Visits? visit)
        {
            try
            {
                if (visit is null)
                {
                    await DisplayAlertAsync("Erro", "Não foi possível registrar a visita.");
                    return;
                }

                await _visitsService.RegisterVisitAsync(visit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar visita: {ex.Message}");
            }
        }
    }
}
