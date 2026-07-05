using ACS_View.Application.DTOs;
using ACS_View.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class VisitBatchPopupViewModel : BaseViewModel
{
    private readonly int houseId;
    private readonly int familyId;

    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private TimeSpan selectedTime = DateTime.Now.TimeOfDay;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string saveButtonText = "Salvar registros";

    public VisitBatchPopupViewModel(int houseId, int familyId, IEnumerable<VisitFamilyMemberOptionDto> people)
    {
        this.houseId = houseId;
        this.familyId = familyId;
        People = new ObservableCollection<VisitBatchPersonOption>(
            people.Select(person => new VisitBatchPersonOption(person, UpdateSaveButtonText)));

        MarkAllCompletedCommand = new Command(() => MarkAll(VisitStatus.Completed));
        MarkAllAbsentCommand = new Command(() => MarkAll(VisitStatus.Absent));
        ClearSelectionCommand = new Command(() => MarkAll(VisitStatus.NoVisit));
        UpdateSaveButtonText();
    }

    public ObservableCollection<VisitBatchPersonOption> People { get; }

    public ICommand MarkAllCompletedCommand { get; }
    public ICommand MarkAllAbsentCommand { get; }
    public ICommand ClearSelectionCommand { get; }

    public bool TryCreateRequest(out VisitBatchRequestDto? request)
    {
        request = null;
        var selectedPeople = People
            .Where(person => person.Status != VisitStatus.NoVisit)
            .Select(person => new VisitBatchPersonRequestDto
            {
                PatientId = person.PatientId,
                Status = person.Status
            })
            .ToList();

        if (selectedPeople.Count == 0)
        {
            ErrorMessage = "Selecione pelo menos uma pessoa para registrar visita.";
            HasError = true;
            return false;
        }

        request = new VisitBatchRequestDto
        {
            HouseId = houseId,
            FamilyId = familyId,
            VisitDate = SelectedDate.Date.Add(SelectedTime),
            Notes = Notes,
            People = selectedPeople
        };

        ErrorMessage = string.Empty;
        HasError = false;
        return true;
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        UpdateSaveButtonText();
    }

    private void MarkAll(VisitStatus status)
    {
        foreach (var person in People)
        {
            person.Status = status;
        }

        UpdateSaveButtonText();
    }

    private void UpdateSaveButtonText()
    {
        var count = People.Count(person => person.Status != VisitStatus.NoVisit);
        SaveButtonText = count == 1 ? "Salvar 1 registro" : $"Salvar {count} registros";
    }
}

public partial class VisitBatchPersonOption : ObservableObject
{
    private readonly Action onStatusChanged;
    private VisitStatus status;

    public VisitBatchPersonOption(VisitFamilyMemberOptionDto person, Action onStatusChanged)
    {
        this.onStatusChanged = onStatusChanged;
        PatientId = person.PatientId;
        PatientName = person.PatientName;
        CareLinesText = string.IsNullOrWhiteSpace(person.CareLinesText)
            ? "Sem categoria monitorada"
            : person.CareLinesText;
        status = person.Status;
    }

    public int PatientId { get; }
    public string PatientName { get; }
    public string CareLinesText { get; }

    public IReadOnlyList<string> StatusOptions { get; } =
    [
        "Sem visita",
        "Realizada",
        "Ausente",
        "Recusada"
    ];

    public string SelectedStatus
    {
        get => StatusToText(Status);
        set
        {
            Status = TextToStatus(value);
            OnPropertyChanged();
        }
    }

    public VisitStatus Status
    {
        get => status;
        set
        {
            if (SetProperty(ref status, value))
            {
                OnPropertyChanged(nameof(SelectedStatus));
                onStatusChanged();
            }
        }
    }

    private static string StatusToText(VisitStatus status)
    {
        return status switch
        {
            VisitStatus.Completed => "Realizada",
            VisitStatus.Absent => "Ausente",
            VisitStatus.Refused => "Recusada",
            _ => "Sem visita"
        };
    }

    private static VisitStatus TextToStatus(string? text)
    {
        return text switch
        {
            "Realizada" => VisitStatus.Completed,
            "Ausente" => VisitStatus.Absent,
            "Recusada" => VisitStatus.Refused,
            _ => VisitStatus.NoVisit
        };
    }
}
