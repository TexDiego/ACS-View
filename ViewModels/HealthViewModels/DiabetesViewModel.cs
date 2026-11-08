using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace ACS_View.ViewModels.HealthViewModels;
public partial class DiabetesViewModel(PatientHealthSession session, IPatientHealthDataService data, IGlucoseMonitoringService glucose, string situation)
    : HealthModuleViewModel(session, "diabetes")
{
    public string Situation { get; } = situation;
    public GlucoseMonitoringEditorViewModel Editor { get; } = new();
    [ObservableProperty] public partial string Estimate { get; set; } = "";
    [ObservableProperty] public partial IReadOnlyList<PatientStripPickup> Pickups { get; set; } = [];
    [ObservableProperty] public partial string HistoryInfo { get; set; } = "";
    [ObservableProperty] public partial DateTime PickupDate { get; set; } = DateTime.Today;
    [ObservableProperty] public partial string PickupQuantity { get; set; } = "";
    [ObservableProperty] public partial string PickupNotes { get; set; } = "";
    [ObservableProperty] public partial bool ShowAllHistory { get; set; }
    private IReadOnlyList<PatientStripPickup> history = [];
    public DateTime Today => DateTime.Today;
    protected override async Task LoadDataAsync()
    {
        var value = await data.GetDiabetesAsync(Session); EnsureActive();
        Editor.Load(value.Monitoring); Estimate = GlucoseMonitoringRules.Estimate(value.Monitoring); history = value.Pickups; UpdateHistory();
    }
    partial void OnShowAllHistoryChanged(bool value) => UpdateHistory();
    private void UpdateHistory() { Pickups = ShowAllHistory ? history : history.Take(5).ToList(); HistoryInfo = history.Count == 0 ? "Nenhuma retirada informada." : $"{history.Count} retirada(s) informada(s). Exibindo {Pickups.Count}."; }
    private bool writing;
    public override bool IsWriting => writing;
    [RelayCommand] private async Task SaveAsync()
    {
        if(writing || IsLoading) return;
        writing = true;
        try { EnsureActive(); var value = Editor.Build(); await glucose.SaveAsync(Session.PatientId, value); EnsureActive(); Session.Invalidate("diabetes"); await LoadAsync(); EnsureActive(); Message = "Rotina habitual salva."; }
        catch(OperationCanceledException) { }
        catch(Exception ex) { if(CanPublish) Message = ex.Message; }
        finally { writing = false; }
    }
    [RelayCommand] private async Task AddPickupAsync()
    {
        if(writing || IsLoading) return;
        writing = true;
        try
        {
            EnsureActive();
            if(!int.TryParse(PickupQuantity, out var quantity) || quantity <= 0) throw new ArgumentException("Informe uma quantidade positiva de fitas.");
            await glucose.AddPickupAsync(Session.PatientId, PickupDate, quantity, PickupNotes); EnsureActive();
            PickupQuantity = ""; PickupNotes = ""; Session.Invalidate("diabetes"); await LoadAsync(); EnsureActive(); Message = "Retirada informada registrada.";
        }
        catch(OperationCanceledException) { }
        catch(Exception ex) { if(CanPublish) Message = ex.Message; }
        finally { writing = false; }
    }
}
