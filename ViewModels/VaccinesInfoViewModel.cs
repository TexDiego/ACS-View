using ACS_View.Application.DTOs;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.ViewModels
{
    public class VaccinesInfoViewModel : BaseViewModel
    {
        private const string NotAvailable = "Informacao nao disponivel";

        public string VaccineName { get; }
        public string DoseName { get; }
        public string AgeLabel { get; }
        public string DiseaseDescription { get; }
        public string Guidance { get; }
        public string Source => VaccineDoseCatalog.Source;
        public string StatusText { get; }
        public string RecommendedText { get; }
        public string DeadlineText { get; }
        public string ApplicationDateText { get; }
        public string LotNumber { get; }
        public string Notes { get; }
        public bool HasApplication { get; }
        public bool HasLotNumber { get; }
        public bool HasNotes { get; }
        public Color StatusColor { get; }

        public VaccinesInfoViewModel(PatientVaccineDoseDto dose)
        {
            VaccineName = dose.Definition.VaccineName;
            DoseName = dose.Definition.DoseLabel;
            Guidance = dose.Definition.Guidance ?? "Conferir esquema com a equipe de imunização.";
            AgeLabel = dose.Definition.AgeLabel ?? FormatRecommendedAge(dose.Definition.MinAgeMonths);
            DiseaseDescription = string.IsNullOrWhiteSpace(dose.Definition.DiseaseDescription)
                ? NotAvailable
                : dose.Definition.DiseaseDescription;
            StatusText = GetStatusText(dose.Status);
            StatusColor = GetStatusColor(dose.Status);
            RecommendedText = dose.HasAgeBasedSchedule ? $"Recomendada em {dose.RecommendedDate:dd/MM/yyyy}" : "Conferir histórico e indicação antes de aprazar";
            DeadlineText = dose.Definition.AdministrationAgeLimitMonthsExclusive is int limit
                ? $"Limite da rotina: antes de completar {limit} meses"
                : dose.HasAgeBasedSchedule && dose.MaximumDate.HasValue
                    ? $"Janela recomendada até: {dose.MaximumDate.Value:dd/MM/yyyy}"
                    : "Aprazamento conforme esquema e avaliação";
            HasApplication = dose.IsApplied;
            ApplicationDateText = dose.ApplicationDate.HasValue
                ? $"Aplicada em {dose.ApplicationDate.Value:dd/MM/yyyy}"
                : dose.IsApplied ? "Aplicada; data não informada" : "Sem registro de aplicacao";
            LotNumber = dose.LotNumber;
            Notes = dose.Notes;
            HasLotNumber = !string.IsNullOrWhiteSpace(LotNumber);
            HasNotes = !string.IsNullOrWhiteSpace(Notes);
        }

        public VaccinesInfoViewModel(string doseKey)
            : this(CreateDoseFromDefinition(doseKey))
        {
        }

        private static PatientVaccineDoseDto CreateDoseFromDefinition(string doseKey)
        {
            var definition = VaccineDoseCatalog.GetDefinition(doseKey)
                ?? new VaccineDoseDefinition(doseKey, NotAvailable, NotAvailable, VaccineSectionType.Child, null, null);

            var today = DateTime.Today;
            return new PatientVaccineDoseDto(
                definition,
                null,
                today,
                null,
                null,
                string.Empty,
                string.Empty,
                VaccineStatus.NotYetDue);
        }

        private static string GetStatusText(VaccineStatus status)
        {
            return status switch
            {
                VaccineStatus.NotYetDue => "Ainda nao indicada",
                VaccineStatus.Due => "Pendente",
                VaccineStatus.Late => "Atrasada",
                VaccineStatus.Applied => "Aplicada",
                VaccineStatus.AppliedLate => "Aplicada com atraso",
                VaccineStatus.RequiresAssessment => "Conferir indicação / histórico",
                VaccineStatus.OutsideRoutine => "Fora da rotina atual",
                _ => "Indefinida"
            };
        }

        private static Color GetStatusColor(VaccineStatus status)
        {
            return status switch
            {
                VaccineStatus.Late => Colors.Red,
                VaccineStatus.AppliedLate => Colors.Orange,
                VaccineStatus.Applied => Colors.Green,
                VaccineStatus.Due => Colors.Goldenrod,
                _ => Colors.Gray
            };
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
