using System.Collections.ObjectModel;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACS_View.ViewModels;

public partial class PhoneEditorViewModel : ObservableObject
{
    public ObservableCollection<Phone> Phones { get; } = [];
    public double ListHeight => Phones.Count == 0 ? 24 : Math.Min(Phones.Count, 4) * 40;
    public PhoneEditorViewModel() => Phones.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ListHeight));
    [ObservableProperty] private string input = string.Empty;
    [ObservableProperty] private string error = string.Empty;
    [ObservableProperty] private Phone? editingPhone;
    [ObservableProperty] private Phone? primaryPhone;
    public bool IsEditing => EditingPhone is not null;
    public bool HasError => !string.IsNullOrEmpty(Error);
    public string ApplyLabel => IsEditing ? "Atualizar telefone" : "Adicionar telefone";

    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInputChanged(string value) => Error = string.Empty;
    partial void OnEditingPhoneChanged(Phone? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(ApplyLabel));
    }
    partial void OnPrimaryPhoneChanged(Phone? value)
    {
        foreach (var phone in Phones) phone.IsPrimary = ReferenceEquals(phone, value);
    }

    public void Load(IEnumerable<Phone> phones)
    {
        Cancel();
        PrimaryPhone = null;
        Phones.Clear();
        foreach (var phone in phones) Phones.Add(new Phone { Id = phone.Id, PatientId = phone.PatientId, Number = phone.Number, IsPrimary = phone.IsPrimary });
        PrimaryPhone = Phones.FirstOrDefault(p => p.IsPrimary);
    }

    [RelayCommand]
    private void Apply() => TryApply();

    public bool TryApply()
    {
        var number = Input.Trim();
        var digits = Phone.Digits(number);
        if (digits.Length == 0) { Error = "Informe um número de telefone."; return false; }
        if (Phones.Any(p => !ReferenceEquals(p, EditingPhone) && Phone.Digits(p.Number) == digits))
        { Error = "Este telefone já está na lista."; return false; }
        if (EditingPhone is not null) EditingPhone.Number = number;
        else
        {
            var phone = new Phone { Number = number };
            Phones.Add(phone);
            if (Phones.Count == 1) PrimaryPhone = phone;
        }
        Cancel();
        return true;
    }

    [RelayCommand]
    private void Edit(Phone? phone)
    {
        if (phone is null || !Phones.Contains(phone)) return;
        EditingPhone = phone;
        Input = phone.Number;
        Error = string.Empty;
    }

    [RelayCommand]
    private void Delete(Phone? phone)
    {
        if (phone is null || !Phones.Remove(phone)) return;
        if (ReferenceEquals(EditingPhone, phone)) Cancel();
        if (ReferenceEquals(PrimaryPhone, phone)) PrimaryPhone = Phones.FirstOrDefault();
    }

    [RelayCommand]
    private void Cancel()
    {
        EditingPhone = null;
        Input = string.Empty;
        Error = string.Empty;
    }
}
