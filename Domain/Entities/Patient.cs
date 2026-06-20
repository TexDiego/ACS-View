using SQLite;

namespace ACS_View.Domain.Entities
{
    public class Patient
    {        
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int FamilyId { get; set; } = -1;
        public int HouseId { get; set; } = -1;

        public string SusNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; } = DateTime.Today;
        public string Observacao { get; set; } = string.Empty;
        public bool BolsaFamilia { get; set; } = false;
    }
}