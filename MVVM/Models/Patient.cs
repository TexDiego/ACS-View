using SQLite;

namespace ACS_View.MVVM.Models
{
    internal class Patient
    {        
        [PrimaryKey]
        public int Id { get; set; }
        public int FamilyId { get; set; }
        public int HouseId { get; set; }

        public string SusNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string Observacao { get; set; } = string.Empty;
        public bool BolsaFamilia { get; set; } = false;
    }
}