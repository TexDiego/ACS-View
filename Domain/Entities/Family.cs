using SQLite;

namespace ACS_View.Domain.Entities
{
    public class Family
    {
        [PrimaryKey]
        public int IdFamilia { get; set; }
        public int IdPessoa { get; set; }
    }
}
