using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.Application.DTOs;

public sealed record BatchSelectionSegment(Guid Id, string Street, int? StartNumber, int? EndNumber, HouseParity Parity, IReadOnlyCollection<int> ResidenceIds, bool IsManual = false)
{
    public string Summary => $"{Street} · {StartNumber?.ToString() ?? "início"}–{EndNumber?.ToString() ?? "fim"} · {Parity switch { HouseParity.Even => "Pares", HouseParity.Odd => "Ímpares", _ => "Todos" }} · {(IsManual ? "seleção individual · " : "")}{ResidenceIds.Count} residências";
}

public sealed class TerritorialSelection
{
    private readonly List<BatchSelectionSegment> segments = [];
    private readonly List<BatchSelectionSegment> manualSegments = [];
    private readonly HashSet<int> manual = [];
    private readonly HashSet<int> excluded = [];
    private readonly HashSet<int> selected = [];
    public IReadOnlySet<int> SelectedIds => selected;
    public IReadOnlyList<BatchSelectionSegment> Segments => segments.Concat(manualSegments).ToArray();

    public void Add(string street, int? start, int? end, HouseParity parity, IEnumerable<int> ids)
    {
        var unique = ids.Distinct().ToArray();
        if (unique.Length == 0) return;
        if (!segments.Any(s => s.Street == street && s.StartNumber == start && s.EndNumber == end && s.Parity == parity && s.ResidenceIds.SequenceEqual(unique)))
            segments.Add(new(Guid.NewGuid(), street, start, end, parity, unique));
        excluded.ExceptWith(unique);
        Rebuild();
    }
    public void Toggle(int id)
    {
        if (selected.Contains(id)) { manual.Remove(id); excluded.Add(id); }
        else { excluded.Remove(id); manual.Add(id); }
        for (var i = manualSegments.Count - 1; i >= 0; i--)
        {
            var segment = manualSegments[i];
            var ids = segment.ResidenceIds.Where(manual.Contains).ToArray();
            if (ids.Length == 0) manualSegments.RemoveAt(i);
            else if (ids.Length != segment.ResidenceIds.Count) manualSegments[i] = segment with { ResidenceIds = ids };
        }
        Rebuild();
    }
    public void Toggle(int id, string street, int? start, int? end, HouseParity parity)
    {
        Toggle(id);
        if (!manual.Contains(id)) return;
        var index = manualSegments.FindIndex(s => s.Street == street && s.StartNumber == start && s.EndNumber == end && s.Parity == parity);
        if (index < 0) manualSegments.Add(new(Guid.NewGuid(), street, start, end, parity, [id], IsManual: true));
        else
        {
            var segment = manualSegments[index];
            manualSegments[index] = segment with { ResidenceIds = segment.ResidenceIds.Append(id).Distinct().ToArray() };
        }
    }
    public void Remove(Guid id)
    {
        var manualSegment = manualSegments.FirstOrDefault(s => s.Id == id);
        if (manualSegment is not null) manual.ExceptWith(manualSegment.ResidenceIds);
        manualSegments.RemoveAll(s => s.Id == id);
        segments.RemoveAll(s => s.Id == id);
        Rebuild();
    }
    public void Clear() { segments.Clear(); manualSegments.Clear(); manual.Clear(); excluded.Clear(); Rebuild(); }
    private void Rebuild()
    {
        selected.Clear(); selected.UnionWith(manual);
        foreach (var segment in segments) selected.UnionWith(segment.ResidenceIds);
        selected.ExceptWith(excluded);
    }
}

public enum VectorRowKind { Sector, Block, Residence }
public sealed record VectorControlRow(VectorRowKind Kind, string Title, int HouseId = 0);

public static class VectorControlHierarchy
{
    public static List<VectorControlRow> Build(IEnumerable<House> houses)
    {
        var rows = new List<VectorControlRow>();
        foreach (var sector in houses.GroupBy(h => TerritorialRules.IsComplete(h) ? h.SetorCensitario!.Trim() : null)
            .OrderBy(g => g.Key == null).ThenBy(g => g.Key, NaturalCodeComparer.Instance))
        {
            rows.Add(new(VectorRowKind.Sector, sector.Key == null ? "Não classificados" : $"Setor {sector.Key}"));
            foreach (var block in sector.GroupBy(h => sector.Key == null ? null : h.NumeroQuadra!.Trim())
                .OrderBy(g => g.Key, NaturalCodeComparer.Instance))
            {
                if (sector.Key != null) rows.Add(new(VectorRowKind.Block, $"Quadra {block.Key}"));
                foreach (var house in block.OrderBy(TerritorialRules.Street, NaturalCodeComparer.Instance)
                    .ThenBy(h => h.NumeroCasa, NaturalCodeComparer.Instance).ThenBy(h => h.Complemento, NaturalCodeComparer.Instance).ThenBy(h => h.CasaId))
                {
                    var partial = sector.Key == null && TerritorialRules.IsClassified(house)
                        ? $" · Setor: {house.SetorCensitario ?? "—"}; quadra: {house.NumeroQuadra ?? "—"}" : "";
                    rows.Add(new(VectorRowKind.Residence, TerritorialRules.Address(house) + partial, house.CasaId));
                }
            }
        }
        return rows;
    }
}
