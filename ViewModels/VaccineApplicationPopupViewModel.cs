using ACS_View.Application.DTOs;

namespace ACS_View.ViewModels;

public partial class VaccineApplicationPopupViewModel : BaseViewModel
{
    private readonly int patientId;
    private readonly PatientVaccineDoseDto dose;
    private DateTime applicationDate;
    private string lotNumber;
    private string notes;
    private string errorMessage = string.Empty;
    private bool hasError;

    public VaccineApplicationPopupViewModel(int patientId, PatientVaccineDoseDto dose)
    {
        this.patientId = patientId;
        this.dose = dose;
        applicationDate = dose.ApplicationDate ?? DateTime.Today;
        lotNumber = dose.LotNumber;
        notes = dose.Notes;
    }

    public string Title => dose.IsApplied ? "Editar aplicação" : "Registrar aplicação";
    public string VaccineTitle => $"{dose.Definition.VaccineName} - {dose.Definition.DoseLabel}";
    public string RecommendedText => dose.MaximumDate.HasValue
        ? $"Recomendada: {dose.RecommendedDate:dd/MM/yyyy} | Prazo: {dose.MaximumDate.Value:dd/MM/yyyy}"
        : $"Recomendada: {dose.RecommendedDate:dd/MM/yyyy}";
    public DateTime MaximumDate => DateTime.Today;

    public DateTime ApplicationDate
    {
        get => applicationDate;
        set
        {
            if (SetProperty(ref applicationDate, value))
            {
                ClearError();
            }
        }
    }

    public string LotNumber
    {
        get => lotNumber;
        set => SetProperty(ref lotNumber, value);
    }

    public string Notes
    {
        get => notes;
        set => SetProperty(ref notes, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public bool HasError
    {
        get => hasError;
        private set => SetProperty(ref hasError, value);
    }

    public bool TryCreateRequest(out VaccineApplicationRequestDto? request)
    {
        request = null;

        if (ApplicationDate.Date > DateTime.Today)
        {
            SetError("A data de aplicação não pode ser futura.");
            return false;
        }

        request = new VaccineApplicationRequestDto(
            patientId,
            dose.Definition.DoseKey,
            ApplicationDate.Date,
            LotNumber ?? string.Empty,
            Notes ?? string.Empty);

        ClearError();
        return true;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}
