using System.Collections.ObjectModel;

namespace ACS_View.MVVM.Models.Services
{
    public class Familia
    {
        public int IdFamily { get; set; }
        public ObservableCollection<HealthRecord> PessoasFamilia { get; set; }
    }
}
