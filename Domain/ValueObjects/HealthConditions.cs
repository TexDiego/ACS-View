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

        public string CID
        {
            get => Cid;
            set => Cid = value;
        }
    }
}
