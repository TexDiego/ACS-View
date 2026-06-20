using SQLite;

namespace ACS_View.Domain.Entities.Health
{
    public class CidGroup
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Code { get; set; } // Ex: "A00-A09"
        public string InitialCode { get; set; }
        public string FinalCode { get; set; }
        public string Description { get; set; }
        public string ChapterCode { get; set; }
    }
}