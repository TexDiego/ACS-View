using SQLite;

namespace ACS_View.Domain.Entities;

public class PatientBolsaFamilia
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed("UX_PatientBolsaFamilia_User_Patient", 1, Unique = true)]
    public int UserId { get; set; }

    [Indexed("UX_PatientBolsaFamilia_User_Patient", 2, Unique = true)]
    public int PatientId { get; set; }

    [Indexed]
    public int ResponsiblePatientId { get; set; }

    public string NisNumber { get; set; } = string.Empty;
}
