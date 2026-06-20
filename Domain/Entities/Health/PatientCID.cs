using SQLite;

namespace ACS_View.Domain.Entities.Health
{
    public class PatientCID
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int CidId { get; set; }
    }
}