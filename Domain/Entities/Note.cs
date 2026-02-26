using SQLite;

namespace ACS_View.Domain.Entities
{
    public class Note
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
