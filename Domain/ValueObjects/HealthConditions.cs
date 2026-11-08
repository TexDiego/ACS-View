using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.Domain.ValueObjects
{
    internal partial class HealthConditions : ObservableObject
    {
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string cid = string.Empty;
        public string ConditionKey { get; set; } = string.Empty;
        public bool IsCid { get; set; }
        [ObservableProperty] private bool selected;
        [ObservableProperty] private string selectedDiabetesType = string.Empty;
        [ObservableProperty] private bool insulinDependentSelected;

        [ObservableProperty] private bool hivSelected;
        [ObservableProperty] private bool immunodeficiencySelected;
        public bool IsImmuneGroup => ConditionKey == HealthConditionCatalog.Imunodeficiente;
        public bool ShowImmuneDetails => IsImmuneGroup && Selected;
        public bool ShowTuberculosisDetails => ConditionKey == HealthConditionCatalog.Tuberculose && Selected;

        public bool IsDiabetesCondition =>
            string.Equals(ConditionKey, HealthConditionCatalog.Diabetes, StringComparison.OrdinalIgnoreCase);

        public bool ShowDiabetesDetails => IsDiabetesCondition && Selected;
        public bool ShowRegistrationDetails => Selected && (IsDiabetesCondition || IsImmuneGroup || ConditionKey == HealthConditionCatalog.Tuberculose);
        public string RegistrationSummary => IsDiabetesCondition
            ? $"{(HasSelectedDiabetesType ? SelectedDiabetesType : "Tipo não informado")} · {(InsulinDependentSelected ? "Insulinodependente" : "Insulina não informada") }"
            : IsImmuneGroup
                ? string.Join(" · ", new[] { ImmunodeficiencySelected ? "Imunodeficiência" : null, HivSelected ? "HIV" : null }.Where(x => x is not null)) is { Length: > 0 } summary ? summary : "Condição não especificada"
                : "Acompanhamento de tuberculose";

        partial void OnHivSelectedChanged(bool value) => OnPropertyChanged(nameof(RegistrationSummary));
        partial void OnImmunodeficiencySelectedChanged(bool value) => OnPropertyChanged(nameof(RegistrationSummary));
        partial void OnInsulinDependentSelectedChanged(bool value) => OnPropertyChanged(nameof(RegistrationSummary));

        public bool HasSelectedDiabetesType => !string.IsNullOrWhiteSpace(SelectedDiabetesType);

        public string CID
        {
            get => Cid;
            set => Cid = value;
        }

        partial void OnSelectedChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowRegistrationDetails));
            OnPropertyChanged(nameof(ShowDiabetesDetails));
            OnPropertyChanged(nameof(ShowImmuneDetails));
            OnPropertyChanged(nameof(ShowTuberculosisDetails));
            if(!value && IsImmuneGroup) { HivSelected = false; ImmunodeficiencySelected = false; }

            if (!value && IsDiabetesCondition)
            {
                SelectedDiabetesType = string.Empty;
                InsulinDependentSelected = false;
            }
        }

        partial void OnSelectedDiabetesTypeChanged(string value)
        {
            OnPropertyChanged(nameof(HasSelectedDiabetesType));
            OnPropertyChanged(nameof(RegistrationSummary));
        }
    }
}
