using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ACS_View.ViewModels;

public partial class TerritorialSearchResult(House house, Action<int> toggle) : ObservableObject
{
    public int Id => house.CasaId;
    public string Address => TerritorialRules.Address(house);
    public string Classification => $"Setor: {house.SetorCensitario ?? "—"} · Quadra: {house.NumeroQuadra ?? "—"}";
    [ObservableProperty] private bool isSelected;
    internal bool Synchronizing { get; set; }
    partial void OnIsSelectedChanged(bool value) { if (!Synchronizing) toggle(Id); }
}

public partial class TerritorialSelectionSegment(BatchSelectionSegment segment) : ObservableObject
{
    public Guid Id => segment.Id;
    [ObservableProperty] private string summary = segment.Summary;
    internal void Update(BatchSelectionSegment current) => Summary = current.Summary;
}

public partial class VetorialControlOptionsViewModel(IHouseService houseService) : BaseViewModel
{
    private readonly TerritorialSelection selection = new();
    private int session = DataChangeTracker.SessionVersion;
    private int resultsVersion = -1;
    private (string Street, int? Start, int? End, HouseParity Parity) criteria;
    [ObservableProperty] private string streetQuery = "";
    [ObservableProperty] private string startNumber = "";
    [ObservableProperty] private string endNumber = "";
    [ObservableProperty] private int parityIndex;
    [ObservableProperty] private string targetCensusSector = "";
    [ObservableProperty] private string targetBlock = "";
    [ObservableProperty] private IReadOnlyList<TerritorialSearchResult> searchResults = Array.Empty<TerritorialSearchResult>();
    public ObservableCollection<TerritorialSelectionSegment> SelectionSegments { get; } = [];
    [ObservableProperty] private int selectedCount;
    [ObservableProperty] private string resultSummary = "Busque uma rua para começar.";
    public string[] ParityOptions { get; } = ["Todos", "Pares", "Ímpares"];

    public void OnAppearing()
    {
        if (session != DataChangeTracker.SessionVersion)
        {
            selection.Clear(); SynchronizeSelection(); SearchResults = Array.Empty<TerritorialSearchResult>();
            TargetCensusSector = TargetBlock = StreetQuery = StartNumber = EndNumber = "";
            session = DataChangeTracker.SessionVersion;
        }
        if (resultsVersion != DataChangeTracker.HousesVersion)
        { SearchResults = Array.Empty<TerritorialSearchResult>(); ResultSummary = "Busque uma rua para atualizar os resultados."; }
    }

    private void RequireSession()
    {
        if (session != DataChangeTracker.SessionVersion) throw new InvalidOperationException("A sessão foi alterada. Reabra a configuração territorial.");
    }

    private static int? ParseBound(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!int.TryParse(value.Trim(), out var number) || number < 0)
            throw new ArgumentException("Informe números inteiros não negativos nos limites do intervalo.");
        return number;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            RequireSession();
            var query = (Street: StreetQuery.Trim(), Start: ParseBound(StartNumber), End: ParseBound(EndNumber), Parity: (HouseParity)Math.Clamp(ParityIndex, 0, 2));
            var version = DataChangeTracker.HousesVersion;
            var houses = await houseService.SearchTerritorialAsync(query.Street, query.Start, query.End, query.Parity);
            RequireSession();
            criteria = query; resultsVersion = version;
            SearchResults = houses.Select(h => new TerritorialSearchResult(h, ToggleResidence)).ToList();
            SynchronizeSelection();
            ResultSummary = $"{houses.Count} residências encontradas";
            if (houses.Count == 0) await DisplayAlertAsync("Busca", "Nenhuma residência encontrada para este trecho.");
        }
        catch (Exception ex) { await DisplayAlertAsync("Erro", ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void ToggleResult(TerritorialSearchResult row) => ToggleResidence(row.Id);

    private void ToggleResidence(int id)
    {
        if (IsBusy)
        {
            SynchronizeSelection();
            return;
        }

        selection.Toggle(id, criteria.Street, criteria.Start, criteria.End, criteria.Parity);
        SynchronizeSelection();
    }

    [RelayCommand]
    private void SelectAllResults()
    {
        if (IsBusy || SearchResults.Count == 0) return;
        selection.Add(criteria.Street, criteria.Start, criteria.End, criteria.Parity, SearchResults.Select(r => r.Id));
        SynchronizeSelection();
    }
    [RelayCommand] private void RemoveSegment(TerritorialSelectionSegment segment)
    { if (!IsBusy) { selection.Remove(segment.Id); SynchronizeSelection(); } }
    [RelayCommand] private void ClearSelection()
    { if (!IsBusy) { selection.Clear(); SynchronizeSelection(); } }

    private void SynchronizeSelection()
    {
        SelectedCount = selection.SelectedIds.Count;
        var segments = selection.Segments;
        // Keep the header's collection and rows alive when a checkbox changes.
        // Replacing ItemsSource rebuilds BindableLayout and can reset the list's scroll.
        foreach (var obsolete in SelectionSegments.Where(row => !segments.Any(s => s.Id == row.Id)).ToArray())
            SelectionSegments.Remove(obsolete);
        foreach (var segment in segments)
        {
            var current = segment with { ResidenceIds = segment.ResidenceIds.Where(selection.SelectedIds.Contains).ToArray() };
            var row = SelectionSegments.FirstOrDefault(item => item.Id == segment.Id);
            if (row is null) SelectionSegments.Add(new TerritorialSelectionSegment(current));
            else row.Update(current);
        }
        foreach (var row in SearchResults)
        { row.Synchronizing = true; row.IsSelected = selection.SelectedIds.Contains(row.Id); row.Synchronizing = false; }
    }

    [RelayCommand]
    private async Task ApplyBatchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            RequireSession();
            if (SelectedCount == 0) throw new ArgumentException("Selecione ao menos uma residência.");
            var sector = TargetCensusSector.Trim(); var block = TargetBlock.Trim();
            if (sector.Length == 0 || block.Length == 0) throw new ArgumentException("Informe o setor censitário e a quadra.");
            var reviewed = await houseService.GetTerritorialSelectionAsync(selection.SelectedIds.ToArray());
            RequireSession();
            var classified = reviewed.Count(TerritorialRules.IsClassified);
            var summary = $"{reviewed.Count} residências selecionadas\n{reviewed.Count - classified} sem classificação; {classified} já classificadas.\n\n"
                + string.Join("\n", SelectionSegments.Select(s => s.Summary))
                + $"\n\nSetor censitário: {sector}\nQuadra: {block}\n\nAs seleções individuais e desmarcações estão incluídas no total acima.";
            var onlyUnclassified = false;
            if (classified > 0)
            {
                var option = await DisplayActionSheetAsync(summary, "Cancelar", null, "Alterar todas as selecionadas", "Alterar apenas residências não classificadas");
                if (option == "Alterar apenas residências não classificadas") onlyUnclassified = true;
                else if (option != "Alterar todas as selecionadas") return;
            }
            var affected = onlyUnclassified ? reviewed.Count - classified : reviewed.Count;
            if (affected == 0) { await DisplayAlertAsync("Revisão", "Nenhuma residência sem classificação no lote."); return; }
            if (!await DisplayConfirmationAsync("Revisar lote", summary + $"\n\n{affected} registros serão alterados. Confirma?", "Aplicar")) return;
            RequireSession();
            var count = await houseService.ApplyTerritoryAsync(reviewed, sector, block, onlyUnclassified);
            selection.Clear(); SynchronizeSelection(); SearchResults = Array.Empty<TerritorialSearchResult>();
            ResultSummary = "Lote aplicado. Busque outro trecho para continuar.";
            await DisplayAlertAsync("Sucesso", $"Classificação territorial atualizada em {count} residências.");
        }
        catch (Exception ex) { await DisplayAlertAsync("Erro", ex.Message); }
        finally { IsBusy = false; }
    }
}
