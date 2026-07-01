using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class BolsaFamiliaPageViewModel(
    IPatientBolsaFamiliaRepository bolsaFamiliaRepository,
    IPatientService patientService,
    IPersonsInfoPopupService personsInfoPopupService) : BaseViewModel
{
    private int _loadedVersion = -1;
    private bool _hasLoaded;
    private bool _skipNextAppearReload;
    private DateTime _suppressReloadUntilUtc = DateTime.MinValue;

    [ObservableProperty] private ObservableCollection<BolsaFamiliaGroup> groups = [];
    [ObservableProperty] private bool hasGroups;
    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private bool isRefreshing = false;
    [ObservableProperty] private int totalRecords;
    [ObservableProperty] private int totalGroups;

    public ICommand PersonInfo => new Command<int>(async id => await OpenPersonInfoAsync(id));
    public ICommand Reload => new Command(async () =>
    {
        IsRefreshing = true;
        await LoadGroupsAsync();
        IsRefreshing = false;
    });

    internal bool ShouldSkipTransientReload()
    {
        if (_skipNextAppearReload || DateTime.UtcNow <= _suppressReloadUntilUtc)
        {
            _skipNextAppearReload = false;
            return true;
        }

        return false;
    }

    public async Task LoadGroupsAsync()
    {
        var currentVersion = DataChangeTracker.PatientsVersion;
        if (_hasLoaded && _loadedVersion == currentVersion)
        {
            return;
        }

        try
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var groupsFromDb = await bolsaFamiliaRepository.GetGroupsAsync();

                Groups.Clear();
                foreach (var group in groupsFromDb)
                {
                    Groups.Add(group);
                }

                HasGroups = Groups.Count > 0;
                IsEmpty = !HasGroups;
                _loadedVersion = currentVersion;
                _hasLoaded = true;
                TotalRecords = Groups.Sum(group => group.BeneficiaryCount);
                TotalGroups = Groups?.Count ?? 0;
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", $"Nao foi possivel carregar os beneficiarios.\n\n{ex.Message}");
        }
    }

    private async Task OpenPersonInfoAsync(int patientId)
    {
        try
        {
            var record = await patientService.GetPatientById(patientId);
            if (record is null)
            {
                await DisplayAlertAsync("Aviso", "Paciente não encontrado.");
                return;
            }

            SuppressTransientReload();
            try
            {
                await personsInfoPopupService.ShowAsync(record);
            }
            finally
            {
                SuppressTransientReload();
            }
        }
        catch
        {
            await DisplayAlertAsync("Erro", "Erro ao carregar os dados da pessoa.", "Fechar");
        }
    }

    private void SuppressTransientReload()
    {
        _skipNextAppearReload = true;
        _suppressReloadUntilUtc = DateTime.UtcNow.AddSeconds(2);
    }
}
