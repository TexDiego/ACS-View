using SQLite;

namespace ACS_View.MVVM.Models
{
    public class HealthRecord
    {
        #region health properties
        [PrimaryKey]
        public string SusNumber { get; set; }
        public int FamilyId { get; set; }
        public int HouseId { get; set; }
        public string Name { get; set; }
        public string? MotherName { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsPregnant { get; set; }
        public bool HasDiabetes { get; set; }
        public bool HasHypertension { get; set; }
        public bool HasTuberculosis { get; set; }
        public bool HasLeprosy { get; set; }
        public bool HasHIV { get; set; }
        public bool HasHeartDesease { get; set; }
        public bool HasKidneyDesease { get; set; }
        public bool HasLungsDesease { get; set; }
        public bool HasLiverDesease { get; set; }
        public bool IsBedridden { get; set; }
        public bool IsHomebound { get; set; }
        public bool HasMentalIllness { get; set; }
        public bool IsNeurodivergent { get; set; }
        public bool IsSmoker { get; set; }
        public bool IsAlcoholic { get; set; }
        public bool IsDrugAddicted { get; set; }
        public bool HasCancer { get; set; }
        public bool HasDisabilities { get; set; }
        public string? Observacao { get; set; }
        public bool BolsaFamilia { get; set; }
        #endregion
                
        // Propriedades derivadas
        [Ignore]
        public bool HasObs => !string.IsNullOrEmpty(Observacao);

        [Ignore]
        public string Idade => CalcularIdadeCompleta(BirthDate);

        [Ignore]
        public bool IsOld => GetAge(BirthDate) >= 60;

        [Ignore]
        public bool IsBaby => GetAge(BirthDate) < 6;

        [Ignore]
        public bool IsChild => GetAge(BirthDate) <= 10;

        [Ignore]
        public bool IsDiabetesAndHypertension => HasDiabetes && HasHypertension;

        [Ignore]
        public string Endereco { get; set; } = "Sem endereço";

        [Ignore]
        public bool HasNothing =>
            IsPregnant || HasDiabetes || HasHypertension || HasTuberculosis || HasLeprosy ||
            HasHIV || HasHeartDesease || HasKidneyDesease || HasLungsDesease || HasLiverDesease ||
            IsBedridden || IsHomebound || HasMentalIllness || IsNeurodivergent || IsOld ||
            IsSmoker || IsAlcoholic || IsDrugAddicted || HasCancer || BolsaFamilia;

        // Métodos para idade
        private static string CalcularIdadeCompleta(DateTime dataNascimento)
        {
            DateTime hoje = DateTime.Today;

            int anos = hoje.Year - dataNascimento.Year;
            int meses = hoje.Month - dataNascimento.Month;
            int dias = hoje.Day - dataNascimento.Day;

            if (dias < 0)
            {
                meses--;
                dias += DateTime.DaysInMonth(hoje.AddMonths(-1).Year, hoje.AddMonths(-1).Month);
            }

            if (meses < 0)
            {
                anos--;
                meses += 12;
            }

            if (anos == 0 && meses == 0 && dias == 0)
                return "Recém-nascido";

            if (anos == 0 && meses == 0)
                return dias == 1 ? "1 dia" : $"{dias} dias";

            if (anos == 0)
                return meses == 1 ? "1 mês" : $"{meses} meses";

            if (anos == 1)
                return meses == 0 ? "1 ano" : meses == 1 ? "1 ano e 1 mês" : $"1 ano e {meses} meses";

            return $"{anos} anos";
        }

        private static int GetAge(DateTime birthDate)
        {
            int age = DateTime.Today.Year - birthDate.Year;

            if (DateTime.Today.Month < birthDate.Month || (DateTime.Today.Month == birthDate.Month && DateTime.Today.Day < birthDate.Day))
            {
                age--;
            }

            return age;
        }
    }
}
