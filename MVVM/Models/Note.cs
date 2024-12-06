using SQLite;

namespace ACS_View.MVVM.Models
{
    public class Note
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
