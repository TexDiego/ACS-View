using SQLite;

namespace ACS_View.Domain.Entities.Health
{
    internal class CidGroup
    {
        [PrimaryKey]
        public string Code { get; set; } // Ex: "A00-A09"
        public string InitialCode { get; set; }
        public string FinalCode { get; set; }
        public string Description { get; set; }
        public string ChapterCode { get; set; }
    }
}