using System.Collections.ObjectModel;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS_View.ViewModels;

public partial class SusNumberEditorViewModel : ObservableObject
{
    public ObservableCollection<SusNumber> Numbers { get; } = [];
    public double ListHeight => Numbers.Count == 0 ? 24 : Math.Min(Numbers.Count, 4) * 40;
    public SusNumberEditorViewModel() => Numbers.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ListHeight));
    [ObservableProperty] private string input = string.Empty;
    [ObservableProperty] private string error = string.Empty;
    [ObservableProperty] private SusNumber? editingNumber;
    [ObservableProperty] private SusNumber? primaryNumber;
    private bool _loading;
    public bool IsDirty { get; private set; }
    public bool IsEditing => EditingNumber is not null;
    public bool HasError => !string.IsNullOrEmpty(Error);
    public string ApplyLabel => IsEditing ? "Atualizar número SUS" : "Adicionar número SUS";

    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInputChanged(string value) => Error = string.Empty;
    partial void OnEditingNumberChanged(SusNumber? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(ApplyLabel));
    }
    partial void OnPrimaryNumberChanged(SusNumber? value)
    {
        foreach (var number in Numbers) number.IsPrimary = ReferenceEquals(number, value);
        if (!_loading) IsDirty = true;
    }

    public void Load(string? original)
    {
        _loading = true;
        try
        {
            Cancel();
            PrimaryNumber = null;
            Numbers.Clear();
            foreach (var number in SusNumberSet.Parse(original)) Numbers.Add(new SusNumber { Number = number });
            PrimaryNumber = Numbers.FirstOrDefault();
            IsDirty = false;
        }
        finally { _loading = false; }
    }

    public void ApplyTo(Patient patient)
    {
        // Preserve raw legacy evidence if this editor has not changed the CNS list.
        if (!IsDirty) return;
        patient.SusNumbers = Numbers.OrderByDescending(n => ReferenceEquals(n, PrimaryNumber)).Select(n => n.Number).ToArray();
        patient.SusNumberRecords = Numbers.OrderByDescending(n => ReferenceEquals(n, PrimaryNumber))
            .Select((n, index) => new PatientSusNumber { Id = n.Id, PatientId = patient.Id, UserId = patient.UserId,
                Number = n.Number, IsPrimary = ReferenceEquals(n, PrimaryNumber), OriginalOrder = index }).ToList();
    }

    public void LoadRecords(IEnumerable<PatientSusNumber> records)
    {
        _loading = true;
        try
        {
            Cancel();
            PrimaryNumber = null;
            Numbers.Clear();
            foreach (var n in records.OrderBy(n => n.OriginalOrder))
                Numbers.Add(new SusNumber { Id = n.Id, PatientId = n.PatientId, Number = n.Number, IsPrimary = n.IsPrimary });
            PrimaryNumber = Numbers.FirstOrDefault(n => n.IsPrimary) ?? Numbers.FirstOrDefault();
            IsDirty = false;
        }
        finally { _loading = false; }
    }

    [RelayCommand]
    private void Apply() => TryApply();

    public bool TryApply()
    {
        var parsed = SusNumberSet.Parse(Input);
        if (parsed.Count != 1) { Error = "Informe um número SUS por vez."; return false; }
        var number = parsed[0];
        if (number.Length != 15) { Error = "O número SUS deve ter 15 dígitos."; return false; }
        if (Numbers.Any(n => !ReferenceEquals(n, EditingNumber) && n.Number == number))
        { Error = "Este número SUS já está na lista."; return false; }
        if (EditingNumber is not null) EditingNumber.Number = number;
        else
        {
            var item = new SusNumber { Number = number };
            Numbers.Add(item);
            if (Numbers.Count == 1) PrimaryNumber = item;
        }
        IsDirty = true;
        Cancel();
        return true;
    }

    [RelayCommand]
    private void Edit(SusNumber? number)
    {
        if (number is null || !Numbers.Contains(number)) return;
        EditingNumber = number;
        Input = number.Number;
        Error = string.Empty;
    }

    [RelayCommand]
    private void Delete(SusNumber? number)
    {
        if (number is null || !Numbers.Remove(number)) return;
        if (ReferenceEquals(EditingNumber, number)) Cancel();
        if (ReferenceEquals(PrimaryNumber, number)) PrimaryNumber = Numbers.FirstOrDefault();
        IsDirty = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        EditingNumber = null;
        Input = string.Empty;
        Error = string.Empty;
    }
}
