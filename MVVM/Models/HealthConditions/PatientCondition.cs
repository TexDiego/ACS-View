using SQLite;

namespace ACS_View.MVVM.Models.HealthConditions
{
    internal class PatientCondition
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int PatientId { get; set; }

        [Indexed]
        public int ConditionId { get; set; }
    }
}