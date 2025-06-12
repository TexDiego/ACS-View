using SQLite;

namespace ACS_View.MVVM.Models
{
    public class Visits
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int HouseId { get; set; }
        public int FamilyId { get; set; }
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }


        public override string ToString()
        {
            return $"Visita: {Id}\nID Casa: {HouseId}\nID Família: {FamilyId}\nData: {Date.ToShortDateString()}\nObservações: {Description}";
        }
    }
}
