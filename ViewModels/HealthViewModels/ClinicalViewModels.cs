using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ACS_View.ViewModels.HealthViewModels;
public abstract partial class ClinicalModuleViewModel(PatientHealthSession s, IPatientHealthDataService data) : HealthModuleViewModel(s, "clinical")
{
    [ObservableProperty] public partial string Details { get; set; } = "";
    protected override async Task LoadDataAsync() { var value = await data.GetClinicalAsync(Session); EnsureActive(); Details = Describe(value); }
    protected abstract string Describe(ClinicalHealthData value);
}
public sealed class HypertensionViewModel(PatientHealthSession s, IPatientHealthDataService data) : ClinicalModuleViewModel(s, data)
{
    protected override string Describe(ClinicalHealthData value) => string.Join("\n", value.Conditions.Where(c => HealthConditionCatalog.GetKey(c.Description) == HealthConditionCatalog.Hipertensao).Select(c => c.Description)) + "\nConsulte o módulo Visitas para indicadores e próximas visitas sugeridas.";
}
