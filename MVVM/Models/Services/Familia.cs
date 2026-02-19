using System.Collections.ObjectModel;

namespace ACS_View.MVVM.Models.Services
{
    internal class Familia
    {
        public int IdFamily { get; set; }
        public ObservableCollection<Patient> PessoasFamilia { get; set; }
    }
}
