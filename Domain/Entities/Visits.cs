using SQLite;

namespace ACS_View.Domain.Entities
{
    public class Visits
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PatientId { get; set; }
        public int HouseId { get; set; }
        public int FamilyId { get; set; }
        public int? BatchId { get; set; }
        public DateTime Date { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


        public override string ToString()
        {
            return $"Visita: {Id}\nID Pessoa: {PatientId}\nID Casa: {HouseId}\nID Família: {FamilyId}\nData: {Date.ToShortDateString()}\nDescrição: {Description}";
        }
    }
}
