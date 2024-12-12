using ACS_View.MVVM.Models.Services;
using SQLite;

namespace ACS_View.MVVM.Models
{
    public class Family
    {
        [PrimaryKey, AutoIncrement, Unique]
        public int IdFamilia { get; set; }
    }
}
