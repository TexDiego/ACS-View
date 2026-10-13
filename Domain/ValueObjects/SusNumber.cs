using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.Domain.ValueObjects;

public sealed class SusNumber : ObservableObject
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    private string number = string.Empty;
    private bool isPrimary;
    public string Number { get => number; set => SetProperty(ref number, value); }
    public bool IsPrimary { get => isPrimary; set => SetProperty(ref isPrimary, value); }
}
