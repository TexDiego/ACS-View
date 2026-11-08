using ACS_View.Application.Health;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace ACS_View.ViewModels.HealthViewModels;
public abstract partial class HealthModuleViewModel(PatientHealthSession session, string cacheKey) : ObservableObject, IDisposable
{
    public virtual bool IsWriting => false;
    protected PatientHealthSession Session { get; } = session;
    private bool disposed;
    private int revision;
    [ObservableProperty] public partial HealthLoadState State { get; set; } = HealthLoadState.NotLoaded;
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string Message { get; set; } = "";
    public async Task LoadAsync(bool retry = false)
    {
        if(disposed) return;
        var version = ++revision;
        if(retry) Session.Invalidate(cacheKey);
        State = HealthLoadState.Loading; IsLoading = true; HasError = false; Message = "";
        try
        {
            await LoadDataAsync();
            if(CanPublish && version == revision) State = HealthLoadState.Loaded;
        }
        catch(OperationCanceledException) { if(CanPublish && version == revision) State = HealthLoadState.NotLoaded; }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            if(CanPublish && version == revision) { State = HealthLoadState.Error; HasError = true; Message = "Não foi possível carregar. Tente novamente."; }
        }
        finally { if(CanPublish && version == revision) IsLoading = false; }
    }
    protected bool CanPublish
    {
        get { try { EnsureActive(); return true; } catch(OperationCanceledException) { return false; } }
    }
    protected void EnsureActive() { Session.EnsureActive(); if(disposed) throw new OperationCanceledException(); }
    protected abstract Task LoadDataAsync();
    [RelayCommand] private Task RetryAsync() => LoadAsync(true);
    public void Dispose() { disposed = true; revision++; }
}
