using ACS_View.Application.DTOs;
using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.ViewModels.HealthViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.ViewModels;

public sealed partial class VaccinationHealthViewModel(
    PatientHealthSession session, IPatientHealthDataService data)
    : HealthModuleViewModel(session, "vaccination")
{
    [ObservableProperty] public partial bool HasSchedule { get; set; }
    [ObservableProperty] public partial VaccineScheduleTotals Totals { get; set; }
    [ObservableProperty] public partial IReadOnlyList<VaccineDoseCardViewModel> Doses { get; set; } = [];

    protected override async Task LoadDataAsync()
    {
        var schedule = await data.GetVaccinationAsync(Session);
        EnsureActive();

        if (schedule is null)
        {
            HasSchedule = false;
            Message = "Cartão vacinal indisponível.";
            return;
        }

        Doses = VaccinesPageViewModel.SortDoses(schedule.Doses)
            .Select(dose => new VaccineDoseCardViewModel(dose))
            .ToList();
        Totals = VaccineScheduleTotals.From(schedule.Doses);
        HasSchedule = true;
        Message = Doses.Count == 0 ? "Nenhuma dose encontrada." : string.Empty;
    }
}
