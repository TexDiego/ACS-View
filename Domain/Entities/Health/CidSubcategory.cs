using SQLite;

namespace ACS_View.Domain.Entities.Health
{
    internal class CidSubcategory
    {
        [PrimaryKey]
        public string Code { get; set; } // Ex: "A00.0"
        public string Description { get; set; }
        public string CategoryCode { get; set; }
    }
}