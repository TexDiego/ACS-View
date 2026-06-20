using SQLite;

namespace ACS_View.Domain.Entities.Health
{
    public class CidCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Code { get; set; } // Ex: "A00"
        public string Description { get; set; }
        public string GroupCode { get; set; }
    }
}
