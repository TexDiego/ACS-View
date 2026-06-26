using SQLite;
using System.ComponentModel.DataAnnotations.Schema;

namespace ACS_View.Domain.Entities.Health
{
    public class PatientConditions
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }

        [ForeignKey(nameof(Patient))]
        public int? PatientId { get; set; }
        public string Description { get; set; }
    }
}
