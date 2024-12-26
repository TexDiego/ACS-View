using SQLite;

namespace ACS_View.MVVM.Models
{
    public class Family
    {
        [PrimaryKey]
        public int IdFamilia { get; set; }
        public int IdPessoa { get; set; }
    }
}
