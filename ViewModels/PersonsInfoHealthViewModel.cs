using ACS_View.Application.Health;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views.HealthViews;
using ACS_View.ViewModels.HealthViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace ACS_View.ViewModels;

public partial class PersonsInfoViewModel : IDisposable
{
    public PatientHealthSession? HealthSession { get; private set; }
    [ObservableProperty] public partial IReadOnlyList<HealthModuleOption> HealthModules { get; set; } = [new("visits", "Visitas")];
    [ObservableProperty] public partial HealthModuleOption? SelectedHealthModule { get; set; }
    [ObservableProperty] public partial View? HealthContent { get; set; }
    [ObservableProperty] public partial bool IsHealthPanelVisible { get; set; }
    [ObservableProperty] public partial string HealthStatus { get; set; } = "";
    private ClinicalHealthData? clinicalData;
    private readonly Dictionary<string, (View View, HealthModuleViewModel Model)> healthViews = [];
    private int moduleSelectionVersion;
    private int contextVersion;

    private void StartHealthSession(Patient patient)
    {
        EndHealthSession();
        WeakReferenceMessenger.Default.Register<PatientHealthChanged>(this, static (recipient, message) =>
        {
            var vm = (PersonsInfoViewModel)recipient;
            if(MainThread.IsMainThread) vm.OnHealthChanged(message);
            else MainThread.BeginInvokeOnMainThread(() => vm.OnHealthChanged(message));
        });
        var owner = currentUser.RequireCurrentUserId();
        var login = DataChangeTracker.SessionVersion;
        HealthSession = new(patient.Id, owner, () => currentUser.CurrentUserId == owner && DataChangeTracker.SessionVersion == login, patient.Name);
        HealthModules = [new("visits", "Visitas")]; SelectedHealthModule = null;
        HealthStatus = "Preparando módulos...";
        _ = PrepareModulesAsync(HealthSession);
    }
    private void OnHealthChanged(PatientHealthChanged message)
    {
        var session = HealthSession;
        if(session is null || message.PatientId != session.PatientId || message.OwnerId != session.OwnerId) return;
        try { session.EnsureActive(); } catch(OperationCanceledException) { return; }
        if(message.Module == "context") { _ = RefreshHealthContextAsync(session); return; }
        session.Invalidate(message.Module);
        dirtyModules.Add(message.Module);
        if(IsHealthPanelVisible && SelectedHealthModule?.Key == message.Module && healthViews.TryGetValue(message.Module, out var module)
            && !module.Model.IsWriting)
        {
            dirtyModules.Remove(message.Module);
            _ = module.Model.LoadAsync();
        }
    }
    private async Task RefreshHealthContextAsync(PatientHealthSession session)
    {
        var versionOfContext = ++contextVersion;
        try
        {
            session.Invalidate("clinical");
            var updated = await healthData.GetClinicalAsync(session);
            var patient = await _patientService.GetPatientById(session.PatientId);
            session.EnsureActive();
            if(HealthSession != session || versionOfContext != contextVersion || patient is null) return;
            var previous = clinicalData;
            var careChanged = previous is null || ClinicalSignature(previous) != ClinicalSignature(updated);
            var visitsChanged = careChanged || PersonInfo.BirthDate != patient.BirthDate;
            session.UpdateContext(new(patient.Id, session.OwnerId, patient.Name));
            PersonInfo = patient; clinicalData = updated;
            HasMotherPatientLink = patient.MotherPatientId is > 0; HasFatherPatientLink = patient.FatherPatientId is > 0;
            if(visitsChanged) session.Invalidate("visits");
            if(careChanged)
            {
                foreach(var module in healthViews.Values) module.Model.Dispose();
                healthViews.Clear(); HealthContent = null;
                HealthModules = HealthModuleRegistry.Definitions.Where(d => d.Applies(updated)).Select(d => new HealthModuleOption(d.Key,d.Title)).ToList();
                var selected = SelectedHealthModule?.Key;
                SelectedHealthModule = null;
                if(IsHealthPanelVisible) SelectedHealthModule = HealthModules.FirstOrDefault(m => m.Key == selected) ?? HealthModules[0];
            }
            else if(visitsChanged) await RefreshVisitsModuleAsync();
            var version = Interlocked.Increment(ref _loadVersion);
            await Task.WhenAll(LoadPatientDetailsAsync(patient.Id, version), LoadVisitFollowUpAsync(patient.Id, version));
            if(FamilyLinks is not null) await FamilyLinks.LoadAsync(patient);
        }
        catch(OperationCanceledException) { }
        catch(Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if(HealthSession == session) HealthStatus = "Não foi possível atualizar o cadastro. Tente novamente."; }
    }
    private static string ClinicalSignature(ClinicalHealthData data) =>
        string.Join("|", data.Conditions.Select(c => c.Description).Order()) + ":" + data.Insulin + ":" + System.Text.Json.JsonSerializer.Serialize(data.Pregnancy?.Pregnancy);
    private readonly HashSet<string> dirtyModules = [];
    private async Task PrepareModulesAsync(PatientHealthSession session)
    {
        var versionOfContext = contextVersion;
        try
        {
            var clinical = await healthData.GetClinicalAsync(session);
            session.EnsureActive();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if(HealthSession != session || versionOfContext != contextVersion) return;
                clinicalData = clinical;
                var selected = SelectedHealthModule?.Key;
                HealthModules = HealthModuleRegistry.Definitions.Where(d => d.Applies(clinical)).Select(d => new HealthModuleOption(d.Key,d.Title)).ToList();
                if(selected is not null) SelectedHealthModule = HealthModules.FirstOrDefault(m => m.Key == selected) ?? HealthModules[0];
                HealthStatus = "";
            });
            if(HealthSession != session || versionOfContext != contextVersion) return;
            await Task.WhenAll(HealthModuleRegistry.Definitions.Where(d => d.Applies(clinical))
                .Select(definition => PreloadModuleAsync(definition, session)));
        }
        catch(OperationCanceledException) { }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await MainThread.InvokeOnMainThreadAsync(() => { if(HealthSession == session) HealthStatus = "Alguns módulos não foram carregados. Tente novamente."; });
        }
    }

    private async Task PreloadModuleAsync(HealthModuleDefinition definition, PatientHealthSession session)
    {
        try { await definition.Preload(session, healthData); }
        catch(OperationCanceledException) { }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if(HealthSession == session) HealthStatus = "Alguns módulos não foram carregados. Abra o módulo para tentar novamente.";
            });
        }
    }
    [RelayCommand] private async Task RetryHealthAsync()
    {
        if(HealthSession is not { } session) return;
        session.Invalidate("clinical"); session.Invalidate("diabetes");
        await PrepareModulesAsync(session);
    }
    [RelayCommand] private void OpenHealth(string? key)
    {
        IsHealthPanelVisible = true;
        var next = HealthModules.FirstOrDefault(m => m.Key == key) ?? SelectedHealthModule ?? HealthModules[0];
        if(SelectedHealthModule == next)
        {
            if(HealthContent is null || dirtyModules.Contains(next.Key)) _ = SelectModuleAsync(next);
        }
        else SelectedHealthModule = next;
    }
    [RelayCommand] private void OpenCondition(HealthIcon? icon)
    {
        if(icon is null) return;
        var module = HealthModuleRegistry.Definitions.FirstOrDefault(d => d.ConditionIcons.Contains(icon.IconSource)
            && (d.ConditionIconKey is null || HealthConditionCatalog.GetKey(icon.Description) == d.ConditionIconKey)
            && HealthModules.Any(option => option.Key == d.Key));
        if(module is not null) OpenHealth(module.Key);
    }
    [RelayCommand] private void CloseHealth() => IsHealthPanelVisible = false;
    partial void OnSelectedHealthModuleChanged(HealthModuleOption? value)
    {
        if(value is not null) _ = SelectModuleAsync(value);
    }
    private async Task SelectModuleAsync(HealthModuleOption option)
    {
        var version = ++moduleSelectionVersion;
        var session = HealthSession; if(session is null) return;
        try
        {
            if(!healthViews.TryGetValue(option.Key, out var module))
            {
                // Visits remains accessible if an independent clinical query failed.
                var clinical = clinicalData ?? (option.Key == "visits" ? new ClinicalHealthData([], false, null) : await healthData.GetClinicalAsync(session));
                session.EnsureActive();
                if(HealthSession != session || version != moduleSelectionVersion) return;
                var definition = HealthModuleRegistry.Definitions.Single(d => d.Key == option.Key);
                module = definition.Create(session, healthData, glucoseMonitoring, infectiousFollowUp, clinical);
                healthViews.Add(option.Key, module);
                HealthContent = module.View;
                await module.Model.LoadAsync();
            }
            else
            {
                HealthContent = module.View;
                if(dirtyModules.Remove(option.Key)) await module.Model.LoadAsync();
            }
        }
        catch(OperationCanceledException) { }
        catch(Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if(HealthSession == session && version == moduleSelectionVersion) HealthStatus = "Não foi possível abrir o módulo. Tente novamente."; }
    }
    private async Task RefreshVisitsModuleAsync()
    {
        if(healthViews.TryGetValue("visits", out var module)) await module.Model.LoadAsync();
    }
    private void EndHealthSession()
    {
        moduleSelectionVersion++; contextVersion++;
        WeakReferenceMessenger.Default.Unregister<PatientHealthChanged>(this);
        dirtyModules.Clear();
        HealthSession?.Dispose(); HealthSession = null;
        foreach(var module in healthViews.Values) module.Model.Dispose();
        healthViews.Clear(); HealthContent = null; clinicalData = null; IsHealthPanelVisible = false;
    }
    public void Dispose() { Interlocked.Increment(ref _loadVersion); Interlocked.Increment(ref _summaryVersion); EndHealthSession(); }
}
