using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ACS_View.ViewModels;

internal partial class ParentPatientPickerViewModel(
    IPatientService patientService,
    int? excludedPatientId,
    string title) : BaseViewModel
{
    private const int SearchDebounceMilliseconds = 250;
    private const int MaxResults = 50;
    private CancellationTokenSource? _searchDebounceCts;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<PatientListItemDto> patients = [];
    [ObservableProperty] private bool hasResults;
    [ObservableProperty] private string emptyMessage = "Digite para buscar um cadastro.";

    public string Title { get; } = title;
    public double Width => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 24;

    public async Task LoadAsync()
    {
        await SearchAsync(SearchText, useDebounce: false);
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchAsync(value, useDebounce: true);
    }

    private async Task SearchAsync(string? search, bool useDebounce)
    {
        var cancellationToken = ResetSearchDebounce();

        try
        {
            if (useDebounce)
            {
                await Task.Delay(SearchDebounceMilliseconds, cancellationToken);
            }

            var result = await patientService.GetPatientListAsync(search, 0, MaxResults);
            cancellationToken.ThrowIfCancellationRequested();

            var items = result.Items
                .Where(patient => patient.Id != excludedPatientId)
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Patients = new ObservableCollection<PatientListItemDto>(items);
                HasResults = Patients.Count > 0;
                EmptyMessage = HasResults
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(search)
                        ? "Nenhum cadastro encontrado."
                        : "Nenhum cadastro encontrado para a busca.";
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Patients = [];
                HasResults = false;
                EmptyMessage = "Não foi possível buscar os cadastros.";
            });
        }
    }

    private CancellationToken ResetSearchDebounce()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        return _searchDebounceCts.Token;
    }
}
