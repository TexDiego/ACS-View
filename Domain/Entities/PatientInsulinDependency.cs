using SQLite;

namespace ACS_View.Domain.Entities;

public class PatientInsulinDependency
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed("UX_PatientInsulinDependency_User_Patient", 1, Unique = true)]
    public int UserId { get; set; }

    [Indexed("UX_PatientInsulinDependency_User_Patient", 2, Unique = true)]
    public int PatientId { get; set; }
}
