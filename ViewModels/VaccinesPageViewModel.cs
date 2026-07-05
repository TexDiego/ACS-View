using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class VaccinesPageViewModel : BaseViewModel
    {
        private readonly IPatientService _patientService;
        private readonly IVaccineService _vaccineService;
        private readonly IPopupService _popupService;
        private int? _loadedPatientId;
        private int _loadVersion;

        [ObservableProperty] private Patient? healthRecord;
        [ObservableProperty] private bool hasPatient;
        [ObservableProperty] private bool showOnlyLate;
        [ObservableProperty] private int appliedCount;
        [ObservableProperty] private int pendingCount;
        [ObservableProperty] private int lateCount;

        public ObservableCollection<VaccineDoseCardViewModel> Doses { get; } = [];
        public ObservableCollection<VaccineDoseCardViewModel> FilteredDoses { get; } = [];

        public ICommand RegisterApplicationCommand { get; }
        public ICommand EditApplicationCommand { get; }
        public ICommand RemoveApplicationCommand { get; }
        public ICommand OpenVaccineInfoCommand { get; }
        public ICommand ToggleLateFilterCommand { get; }

        public string LateFilterText => ShowOnlyLate ? "Mostrar todas" : "Apenas atrasadas";

        public VaccinesPageViewModel(IPatientService patientService, IVaccineService vaccineService, IPopupService popupService)
        {
            _patientService = patientService;
            _vaccineService = vaccineService;
            _popupService = popupService;

            RegisterApplicationCommand = new Command<VaccineDoseCardViewModel>(async dose => await OpenApplicationPopupAsync(dose));
            EditApplicationCommand = new Command<VaccineDoseCardViewModel>(async dose => await OpenApplicationPopupAsync(dose));
            RemoveApplicationCommand = new Command<VaccineDoseCardViewModel>(async dose => await RemoveApplicationAsync(dose));
            OpenVaccineInfoCommand = new Command<VaccineDoseCardViewModel>(async dose => await OpenVaccineInfoAsync(dose));
            ToggleLateFilterCommand = new Command(() => ShowOnlyLate = !ShowOnlyLate);
        }

        public async Task LoadPatientAsync(int patientId)
        {
            if (patientId < 0)
            {
                await DisplayAlertAsync("Erro", "Paciente invalido.", "Voltar");
                return;
            }

            if (_loadedPatientId == patientId && HealthRecord is not null && Doses.Count > 0)
            {
                return;
            }

            var loadVersion = Interlocked.Increment(ref _loadVersion);
            HealthRecord = null;
            HasPatient = false;
            IsLoading = true;
            Doses.Clear();
            FilteredDoses.Clear();
            RefreshSummaries();

            try
            {
                var patient = await _patientService.GetPatientById(patientId);

                if (loadVersion != _loadVersion)
                {
                    return;
                }

                if (patient is null)
                {
                    await DisplayAlertAsync("Erro", "Paciente nao encontrado.", "Voltar");
                    return;
                }

                var schedule = await _vaccineService.GetScheduleForPatientAsync(patientId);

                if (loadVersion != _loadVersion)
                {
                    return;
                }

                if (schedule is null)
                {
                    await DisplayAlertAsync("Erro", "Cartao vacinal nao encontrado.", "Voltar");
                    return;
                }

                HealthRecord = patient;
                foreach (var dose in SortDoses(schedule.Doses))
                {
                    Doses.Add(new VaccineDoseCardViewModel(dose));
                }

                HasPatient = true;
                _loadedPatientId = patientId;
                RefreshSummaries();
                RefreshFilteredDoses();
            }
            finally
            {
                if (loadVersion == _loadVersion)
                {
                    IsLoading = false;
                }
            }
        }

        partial void OnShowOnlyLateChanged(bool value)
        {
            OnPropertyChanged(nameof(LateFilterText));
            RefreshFilteredDoses();
        }

        private async Task OpenApplicationPopupAsync(VaccineDoseCardViewModel? card)
        {
            if (card is null || _loadedPatientId is not int patientId || HealthRecord is null)
            {
                return;
            }

            var result = await _popupService.ShowAsync<VaccineApplicationRequestDto>(
                new VaccineApplicationPopup(patientId, card.Dose));

            if (result.WasDismissed || result.Result is not VaccineApplicationRequestDto request)
            {
                return;
            }

            await ApplyApplicationOptimisticallyAsync(card, request);
        }

        private async Task OpenVaccineInfoAsync(VaccineDoseCardViewModel? card)
        {
            if (card is null)
            {
                return;
            }

            await _popupService.ShowAsync(new VaccinesInfo(card.Dose));
        }

        private async Task ApplyApplicationOptimisticallyAsync(VaccineDoseCardViewModel card, VaccineApplicationRequestDto request)
        {
            if (HealthRecord is null)
            {
                return;
            }

            var previous = card.Dose;
            var optimistic = CreateDoseDto(previous, request.ApplicationDate, request.LotNumber, request.Notes);
            UpdateCard(card, optimistic);

            try
            {
                await _vaccineService.ApplyDoseAsync(request);
            }
            catch (Exception ex)
            {
                UpdateCard(card, previous);
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private async Task RemoveApplicationAsync(VaccineDoseCardViewModel? card)
        {
            if (card is null || _loadedPatientId is not int patientId || !card.IsApplied)
            {
                return;
            }

            var shouldRemove = await DisplayConfirmationAsync(
                "Remover registro",
                "Deseja remover a aplicacao registrada para esta dose?",
                "Remover");

            if (!shouldRemove)
            {
                return;
            }

            var previous = card.Dose;
            var optimistic = CreateDoseDto(previous, null, string.Empty, string.Empty);
            UpdateCard(card, optimistic);

            try
            {
                await _vaccineService.RemoveDoseApplicationAsync(patientId, previous.Definition.DoseKey);
            }
            catch (Exception ex)
            {
                UpdateCard(card, previous);
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private PatientVaccineDoseDto CreateDoseDto(
            PatientVaccineDoseDto previous,
            DateTime? applicationDate,
            string lotNumber,
            string notes)
        {
            var status = VaccineStatusCalculator.Calculate(
                HealthRecord!.BirthDate,
                DateTime.Today,
                previous.Definition,
                applicationDate);

            return previous with
            {
                ApplicationDate = applicationDate?.Date,
                LotNumber = lotNumber.Trim(),
                Notes = notes.Trim(),
                Status = status
            };
        }

        private void UpdateCard(VaccineDoseCardViewModel card, PatientVaccineDoseDto dose)
        {
            card.Dose = dose;
            RefreshSummaries();
            RefreshFilteredDoses();
        }

        private void RefreshSummaries()
        {
            AppliedCount = Doses.Count(dose => dose.Status is VaccineStatus.Applied or VaccineStatus.AppliedLate);
            LateCount = Doses.Count(dose => dose.Status == VaccineStatus.Late);
            PendingCount = Doses.Count(dose => dose.Status == VaccineStatus.Due);
        }

        private void RefreshFilteredDoses()
        {
            FilteredDoses.Clear();
            var source = ShowOnlyLate
                ? Doses.Where(dose => dose.Status == VaccineStatus.Late)
                : Doses;

            foreach (var dose in source.OrderBy(dose => GetStatusSort(dose.Status)).ThenBy(dose => dose.RecommendedDate))
            {
                FilteredDoses.Add(dose);
            }
        }

        private static IEnumerable<PatientVaccineDoseDto> SortDoses(IEnumerable<PatientVaccineDoseDto> doses)
        {
            return doses
                .OrderBy(dose => GetStatusSort(dose.Status))
                .ThenBy(dose => dose.RecommendedDate)
                .ThenBy(dose => dose.Definition.VaccineName)
                .ThenBy(dose => dose.Definition.DoseLabel);
        }

        private static int GetStatusSort(VaccineStatus status)
        {
            return status switch
            {
                VaccineStatus.Late => 0,
                VaccineStatus.Due => 1,
                VaccineStatus.NotYetDue => 2,
                VaccineStatus.AppliedLate => 3,
                VaccineStatus.Applied => 4,
                _ => 5
            };
        }
    }

    public partial class VaccineDoseCardViewModel : ObservableObject
    {
        [ObservableProperty] private PatientVaccineDoseDto dose;

        public VaccineDoseCardViewModel(PatientVaccineDoseDto dose)
        {
            this.dose = dose;
        }

        public VaccineStatus Status => Dose.Status;
        public string VaccineName => Dose.Definition.VaccineName;
        public string DoseName => Dose.Definition.DoseLabel;
        public string AgeLabel => Dose.Definition.AgeLabel ?? FormatRecommendedAge(Dose.Definition.MinAgeMonths);
        public DateTime RecommendedDate => Dose.RecommendedDate;
        public bool IsApplied => Dose.IsApplied;
        public bool CanRemove => Dose.IsApplied;
        public string PrimaryActionText => Dose.IsApplied ? "Editar aplicacao" : "Registrar aplicacao";
        public string StatusText => Dose.Status switch
        {
            VaccineStatus.NotYetDue => "Ainda nao indicada",
            VaccineStatus.Due => "Pendente",
            VaccineStatus.Late => "Atrasada",
            VaccineStatus.Applied => "Aplicada",
            VaccineStatus.AppliedLate => "Aplicada com atraso",
            _ => "Indefinida"
        };
        public string ApplicationDateText => Dose.ApplicationDate.HasValue
            ? $"Aplicada em {Dose.ApplicationDate.Value:dd/MM/yyyy}"
            : "Sem registro de aplicacao";
        public string DeadlineText => Dose.MaximumDate.HasValue
            ? $"Prazo maximo: {Dose.MaximumDate.Value:dd/MM/yyyy}"
            : $"Recomendada em: {Dose.RecommendedDate:dd/MM/yyyy}";
        public string NotesText => string.IsNullOrWhiteSpace(Dose.Notes) ? string.Empty : Dose.Notes;
        public bool HasNotes => !string.IsNullOrWhiteSpace(Dose.Notes);
        public Color StatusColor => Dose.Status switch
        {
            VaccineStatus.Late => Colors.Red,
            VaccineStatus.AppliedLate => Colors.Orange,
            VaccineStatus.Applied => Colors.Green,
            VaccineStatus.Due => Colors.Goldenrod,
            _ => Colors.Gray
        };

        partial void OnDoseChanged(PatientVaccineDoseDto value)
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(VaccineName));
            OnPropertyChanged(nameof(DoseName));
            OnPropertyChanged(nameof(AgeLabel));
            OnPropertyChanged(nameof(RecommendedDate));
            OnPropertyChanged(nameof(IsApplied));
            OnPropertyChanged(nameof(CanRemove));
            OnPropertyChanged(nameof(PrimaryActionText));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ApplicationDateText));
            OnPropertyChanged(nameof(DeadlineText));
            OnPropertyChanged(nameof(NotesText));
            OnPropertyChanged(nameof(HasNotes));
            OnPropertyChanged(nameof(StatusColor));
        }

        private static string FormatRecommendedAge(int? months)
        {
            if (!months.HasValue)
            {
                return "Qualquer idade";
            }

            if (months.Value == 0)
            {
                return "Ao nascer";
            }

            if (months.Value < 12)
            {
                return $"{months.Value} meses";
            }

            return months.Value % 12 == 0
                ? $"{months.Value / 12} anos"
                : $"{months.Value} meses";
        }
    }
}
