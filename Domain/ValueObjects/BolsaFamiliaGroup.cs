using System.Collections.ObjectModel;

namespace ACS_View.Domain.ValueObjects;

public class BolsaFamiliaGroup
{
    public int ResponsiblePatientId { get; set; }
    public string ResponsibleName { get; set; } = string.Empty;
    public int BeneficiaryCount { get; set; }
    public ObservableCollection<BolsaFamiliaBeneficiary> Beneficiaries { get; set; } = [];
}
