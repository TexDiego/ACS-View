using SQLite;

namespace ACS_View.MVVM.Models
{
    public class HealthRecord
    {
        [PrimaryKey, Unique]
        public string SusNumber { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsPregnant { get; set; }
        public bool HasDiabetes { get; set; }
        public bool HasHypertension { get; set; }
        public bool HasTuberculosis { get; set; }
        public bool IsDiabetesAndHypertension => HasDiabetes && HasHypertension;
        public bool HasLeprosy { get; set; }
        public bool IsBedridden { get; set; }
        public bool IsHomebound { get; set; }
        public bool HasMentalIllness { get; set; }
        public bool IsSmoker { get; set; }
        public bool HasCancer { get; set; }
        public bool IsOld => VerificarIdade60AnosOuMais(BirthDate);
        public bool IsBaby => VerificarIdadeMenor2Anos(BirthDate);
        public bool HasDisabilities { get; set; }
        public string? Observacao { get; set; }
        public bool HasObs { get; set; }

        static bool VerificarIdade60AnosOuMais(DateTime dataNascimento)
        {
            DateTime hoje = DateTime.Today;
            int idade = hoje.Year - dataNascimento.Year;

            if (hoje.Month < dataNascimento.Month || (hoje.Month == dataNascimento.Month && hoje.Day < dataNascimento.Day))
            {
                idade--;
            }

            return idade >= 60;
        }

        static bool VerificarIdadeMenor2Anos(DateTime dataNascimento)
        {
            DateTime hoje = DateTime.Today;
            int idade = hoje.Year - dataNascimento.Year;

            if (hoje.Month < dataNascimento.Month || (hoje.Month == dataNascimento.Month && hoje.Day < dataNascimento.Day))
            {
                idade--;
            }

            return idade < 2;
        }
    }
}
