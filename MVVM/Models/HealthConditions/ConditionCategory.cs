using SQLite;

namespace ACS_View.MVVM.Models.HealthConditions
{
    internal class ConditionCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}