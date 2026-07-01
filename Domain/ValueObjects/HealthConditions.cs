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

        public bool IsDiabetesCondition =>
            string.Equals(ConditionKey, HealthConditionCatalog.Diabetes, StringComparison.OrdinalIgnoreCase);

        public bool ShowDiabetesDetails => IsDiabetesCondition && Selected;
        public bool HasSelectedDiabetesType => !string.IsNullOrWhiteSpace(SelectedDiabetesType);

        public string CID
        {
            get => Cid;
            set => Cid = value;
        }

        partial void OnSelectedChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowDiabetesDetails));

            if (!value && IsDiabetesCondition)
            {
                SelectedDiabetesType = string.Empty;
                InsulinDependentSelected = false;
            }
        }

        partial void OnSelectedDiabetesTypeChanged(string value)
        {
            OnPropertyChanged(nameof(HasSelectedDiabetesType));
        }
    }
}
