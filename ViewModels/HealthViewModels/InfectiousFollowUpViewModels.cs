using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.Input;
namespace ACS_View.ViewModels.HealthViewModels;

public abstract partial class InfectiousFollowUpViewModel(PatientHealthSession session, IPatientHealthDataService data,
    IPatientInfectiousFollowUpService service, string conditionCode, string title) : HealthModuleViewModel(session, conditionCode)
{
    public string Title { get; } = title;
    public InfectiousFollowUpEditorViewModel Editor { get; } = new(conditionCode);
    private bool writing;
    public override bool IsWriting => writing;
    protected override async Task LoadDataAsync()
    {
        var value = await data.GetInfectiousAsync(Session, conditionCode); EnsureActive(); Editor.Load(value);
    }
    [RelayCommand] private async Task SaveAsync()
    {
        if(writing || IsLoading) return;
        writing = true;
        try
        {
            EnsureActive(); await service.SaveAsync(Session.PatientId, Editor.Build()); EnsureActive();
            Session.Invalidate(conditionCode); await LoadAsync(); EnsureActive(); Message = "Acompanhamento salvo.";
        }
        catch(OperationCanceledException) { }
        catch(Exception ex) { if(CanPublish) Message = ex.Message; }
        finally { writing = false; }
    }
}
public sealed class TuberculosisViewModel(PatientHealthSession s, IPatientHealthDataService d, IPatientInfectiousFollowUpService service)
    : InfectiousFollowUpViewModel(s,d,service,InfectiousFollowUpRules.Tuberculosis, "Tuberculose") { }
public sealed class HivViewModel(PatientHealthSession s, IPatientHealthDataService d, IPatientInfectiousFollowUpService service)
    : InfectiousFollowUpViewModel(s,d,service,InfectiousFollowUpRules.Hiv, "HIV") { }
public sealed class ImmunodeficiencyViewModel(PatientHealthSession s, IPatientHealthDataService d, IPatientInfectiousFollowUpService service)
    : InfectiousFollowUpViewModel(s,d,service,InfectiousFollowUpRules.Immunodeficiency, "Imunodeficiência") { }
