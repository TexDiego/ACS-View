using SQLite;

namespace ACS_View.MVVM.Models.HealthConditions
{
    internal class Condition
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        [Indexed]
        public int CategoryId { get; set; }
    }
}