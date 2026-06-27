using SQLite;

namespace ACS_View.Domain.Entities
{
    public class Patient
    {        
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int FamilyId { get; set; } = -1;
        public int HouseId { get; set; } = -1;

        public string SusNumber { get; set; } = string.Empty;
        public string? FamilyResponsibleSus { get; set; }
        public int? MotherPatientId { get; set; }
        public int? FatherPatientId { get; set; }
        public int? FamilyResponsiblePatientId { get; set; }
        public string Sexo { get; set; } = "Indeterminado";
        public string Name { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string SearchName { get; set; } = string.Empty;
        public string SearchMotherName { get; set; } = string.Empty;
        public string SearchFatherName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; } = DateTime.Today;
        public string Observacao { get; set; } = string.Empty;
        public bool BolsaFamilia { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string StatusReason { get; set; } = string.Empty;
        public DateTime? StatusChangedAt { get; set; }
    }
}
