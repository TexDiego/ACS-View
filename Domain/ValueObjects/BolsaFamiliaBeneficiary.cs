namespace ACS_View.Domain.ValueObjects;

public class BolsaFamiliaBeneficiary
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string SusNumber { get; set; } = string.Empty;
    public string NisNumber { get; set; } = string.Empty;
    public bool HasNis => !string.IsNullOrWhiteSpace(NisNumber);
}
