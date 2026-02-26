using SQLite;

namespace ACS_View.Domain.Entities.Health
{
    internal class CidCategory
    {
        [PrimaryKey]
        public string Code { get; set; } // Ex: "A00"
        public string Description { get; set; }
        public string GroupCode { get; set; }
    }
}
