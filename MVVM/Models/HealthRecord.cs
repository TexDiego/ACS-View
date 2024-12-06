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
        public bool IsOld => GetAge(BirthDate) >= 60;
        public bool IsBaby => GetAge(BirthDate) < 2;
        public bool HasDisabilities { get; set; }
        public string? Observacao { get; set; }
        public bool HasObs { get; set; }

        static int GetAge(DateTime birthDate)
        {
            DateTime today = DateTime.Today;

            var age = DateTime.Today.Year - birthDate.Year;

            if (DateTime.Today.Month < birthDate.Month || (DateTime.Today.Month == birthDate.Month && DateTime.Today.Day < birthDate.Day))
            {
                age--;
            }

            return age;
        }
    }
}
