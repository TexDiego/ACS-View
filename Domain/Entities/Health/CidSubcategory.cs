using SQLite;

namespace ACS_View.Domain.Entities.Health
{
    public class CidSubcategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Code { get; set; } // Ex: "A00.0"
        public string Description { get; set; }
        public string CategoryCode { get; set; }
    }
}