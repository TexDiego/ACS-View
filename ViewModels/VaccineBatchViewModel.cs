using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS_View.ViewModels;

public partial class VaccineBatchViewModel : BaseViewModel
{
    private readonly IVaccineService _vaccineService;
    private readonly int _patientId;
    private readonly int _userId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; private set; } = string.Empty;

    public VaccineBatchViewModel(IVaccineService vaccineService, Patient patient, IEnumerable<PatientVaccineDoseDto> doses)
    {
        _vaccineService = vaccineService;
        _patientId = patient.Id;
        _userId = patient.UserId;
        PatientName = patient.Name;
        Doses = doses.Select(dose => new VaccineBatchDoseViewModel(dose)).ToArray();
    }

    public string PatientName { get; }
    public IReadOnlyList<VaccineBatchDoseViewModel> Doses { get; }
    public bool HasError => ErrorMessage.Length > 0;

    [RelayCommand]
    private void UseToday()
    {
        var today = DateTime.Today;
        foreach (var dose in Doses)
        {
            dose.ApplicationDate = today;
        }

        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void UseRecommendedDates()
    {
        foreach (var dose in Doses)
        {
            dose.ApplicationDate = dose.RecommendedDate;
        }

        ErrorMessage = string.Empty;
    }

    public async Task<bool> SaveAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        ErrorMessage = string.Empty;
        var dates = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var dose in Doses)
        {
            if (dose.ApplicationDate is not DateTime date)
            {
                ErrorMessage = $"Informe a data de aplicação de {dose.Title}.";
                return false;
            }

            dates.Add(dose.DoseKey, date);
        }

        IsBusy = true;
        try
        {
            await _vaccineService.ApplyOverdueDosesAsync(_patientId, _userId, dates);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class VaccineBatchDoseViewModel : ObservableObject
{
    [ObservableProperty] public partial DateTime? ApplicationDate { get; set; }

    public VaccineBatchDoseViewModel(PatientVaccineDoseDto dose)
    {
        DoseKey = dose.Definition.DoseKey;
        Title = $"{dose.Definition.VaccineName} · {dose.Definition.DoseLabel}";
        RecommendedDate = dose.RecommendedDate;
        ApplicationDate = RecommendedDate;
    }

    public string DoseKey { get; }
    public string Title { get; }
    public DateTime RecommendedDate { get; }
    public DateTime MaximumDate => DateTime.Today;
}
