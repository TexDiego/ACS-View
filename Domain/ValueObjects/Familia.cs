using ACS_View.Domain.Entities;
using System.Collections.ObjectModel;

namespace ACS_View.Domain.ValueObjects
{
    public class Familia
    {
        public int IdFamily { get; set; }
        public ObservableCollection<Patient> PessoasFamilia { get; set; } = [];
    }
}
