using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using VaccinationHealthViewModel = ACS_View.ViewModels.VaccinationHealthViewModel;
using ACS_View.ViewModels.HealthViewModels;
namespace ACS_View.Views.HealthViews;
public sealed record HealthModuleDefinition(string Key, string Title, Func<ClinicalHealthData, bool> Applies,
    Func<PatientHealthSession, IPatientHealthDataService, Task> Preload,
    Func<PatientHealthSession, IPatientHealthDataService, IGlucoseMonitoringService, IPatientInfectiousFollowUpService, ClinicalHealthData, (View View, HealthModuleViewModel Model)> Create)
{
    public bool RequiresChild { get; init; }
    public bool IsAvailable(ClinicalHealthData data, PatientHealthContext context) =>
        (!RequiresChild || context.IsUnderTwelve) && Applies(data);

    public IReadOnlyList<string> ConditionIcons { get; init; } = [];
    public string? ConditionIconKey { get; init; }
}
public static class HealthModuleRegistry
{
    private static bool Has(ClinicalHealthData c, string key) => c.Conditions.Any(x => HealthConditionCatalog.GetKey(x.Description) == key);
    public static IReadOnlyList<HealthModuleDefinition> Definitions { get; } = [
        new("visits", "Visitas", _ => true, (s,d) => d.GetVisitsAsync(s), (s,d,g,f,c) => Bind(new VisitsView(), new VisitsViewModel(s,d))),
        new("vaccination", "Vacinação", _ => true,
            (session, data) => data.GetVaccinationAsync(session),
            (session, data, _, _, _) => Bind(new VaccinationHealthView(), new VaccinationHealthViewModel(session, data)))
            { RequiresChild = true, ConditionIcons = ["criancas.png"] },
        new("diabetes", "Diabetes / insulina", c => Has(c, HealthConditionCatalog.Diabetes) || c.Insulin, (s,d) => d.GetDiabetesAsync(s), (s,d,g,f,c) => Bind(new DiabetesView(), new DiabetesViewModel(s,d,g,
            string.Join("\n", c.Conditions.Where(x => HealthConditionCatalog.GetKey(x.Description) == HealthConditionCatalog.Diabetes).Select(x => x.Description)) + (c.Insulin ? "\nInsulinoterapia informada." : "\nInsulinoterapia não informada.")))) { ConditionIcons = ["diabetes.png", "insulina.png"] },
        new("hypertension", "Hipertensão", c => Has(c, HealthConditionCatalog.Hipertensao), (s,d) => d.GetClinicalAsync(s), (s,d,g,f,c) => Bind(new HypertensionView(), new HypertensionViewModel(s,d))) { ConditionIcons = ["hipertensao.png"] },
        new("pregnancy", "Gestação / puerpério", c => Has(c, HealthConditionCatalog.Gestante) || c.Pregnancy?.Pregnancy.Status == Domain.Enums.PregnancyStatus.Active || (c.Pregnancy is not null && PregnancyCalculator.IsPuerperal(c.Pregnancy.Pregnancy)), (s,d) => d.GetClinicalAsync(s), (s,d,g,f,c) => Bind(new PregnancyHealthView(), new PregnancyHealthViewModel(s,d))) { ConditionIcons = ["gestante.png", "puerpera.png"] },
        new("tuberculosis", "Tuberculose", c => Has(c, HealthConditionCatalog.Tuberculose), (s,d) => d.GetInfectiousAsync(s,"tuberculosis"),
            (s,d,g,f,c) => Bind(new TuberculosisView(), new TuberculosisViewModel(s,d,f))) { ConditionIcons = ["tuberculose.png"] },
        new(InfectiousFollowUpRules.Hiv, "HIV", c => Has(c, HealthConditionCatalog.Hiv),
            (s,d) => d.GetInfectiousAsync(s, InfectiousFollowUpRules.Hiv),
            (s,d,g,f,c) => Bind(new ImmuneFollowUpView(), new HivViewModel(s,d,f))) { ConditionIcons = ["hiv.png"] },
        new(InfectiousFollowUpRules.Immunodeficiency, "Imunodeficiência", c => Has(c, HealthConditionCatalog.Imunodeficiente),
            (s,d) => d.GetInfectiousAsync(s, InfectiousFollowUpRules.Immunodeficiency),
            (s,d,g,f,c) => Bind(new ImmuneFollowUpView(), new ImmunodeficiencyViewModel(s,d,f)))
            { ConditionIcons = ["hematologicas.png"], ConditionIconKey = HealthConditionCatalog.Imunodeficiente }
    ];
    private static (View, HealthModuleViewModel) Bind(View view, HealthModuleViewModel vm) { view.BindingContext = vm; return (view, vm); }
}
