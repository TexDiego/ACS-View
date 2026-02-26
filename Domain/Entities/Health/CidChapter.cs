using SQLite;

namespace ACS_View.Domain.Entities.Health
{
    internal class CidChapter
    {
        [PrimaryKey]
        public string Code { get; set; } // Ex: "I"
        public string InitialCode { get; set; }
        public string FinalCode { get; set; }
        public string Description { get; set; }
    }
}