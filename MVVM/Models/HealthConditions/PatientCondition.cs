using SQLite;

namespace ACS_View.MVVM.Models.HealthConditions
{
    internal class PatientCondition
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Name = "UX_PatientCondition", Order = 1, Unique = true)]
        public int PatientId { get; set; }

        [Indexed(Name = "UX_PatientCondition", Order = 2, Unique = true)]
        public int ConditionId { get; set; }

    }
}