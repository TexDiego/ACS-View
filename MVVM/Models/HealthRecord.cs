using SQLite;

namespace ACS_View.MVVM.Models
{
    public class HealthRecord()
    {
        [PrimaryKey]
        public string SusNumber { get; set; }
        public int FamilyId { get; set; }
        public int HouseId { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsPregnant { get; set; }
        public bool HasDiabetes { get; set; }
        public bool HasHypertension { get; set; }
        public bool HasTuberculosis { get; set; }
        public bool HasLeprosy { get; set; }
        public bool IsBedridden { get; set; }
        public bool IsHomebound { get; set; }
        public bool HasMentalIllness { get; set; }
        public bool IsSmoker { get; set; }
        public bool HasCancer { get; set; }
        public bool HasDisabilities { get; set; }
        public string? Observacao { get; set; }
        public bool HasObs { get; set; }

        // Vacinas
        public bool BCG { get; set; }
        public bool HepatitisBAoNascer { get; set; }
        public bool Penta1 { get; set; }
        public bool VIP1 { get; set; }
        public bool Pneumo10_1 { get; set; }
        public bool VRH1 { get; set; }
        public bool MeningoC1 { get; set; }
        public bool Penta2 { get; set; }
        public bool VIP2 { get; set; }
        public bool Pneumo10_2 { get; set; }
        public bool VRH2 { get; set; }
        public bool MeningoC2 { get; set; }
        public bool Penta3 { get; set; }
        public bool VIP3 { get; set; }
        public bool Covid1 { get; set; }
        public bool Covid2 { get; set; }
        public bool FebreAmarela1 { get; set; }
        public bool Pneumo10_3 { get; set; }
        public bool MeningoC3 { get; set; }
        public bool TripliceViral { get; set; }
        public bool DTP1 { get; set; }
        public bool VIP4 { get; set; }
        public bool HepatiteA { get; set; }
        public bool TetraViral { get; set; }
        public bool DTP2 { get; set; }
        public bool FebreAmarela2 { get; set; }
        public bool Varicela { get; set; }
        public bool FebreAmarela3 { get; set; }
        public bool Pneumo23 { get; set; }
        public bool DT { get; set; }
        public bool HPV { get; set; }

        // Propriedades de exibição de vacinas
        public bool ShowRN => GetMonth(BirthDate) >= 0;
        public bool Show2Meses => GetMonth(BirthDate) >= 2;
        public bool Show3Meses => GetMonth(BirthDate) >= 3;
        public bool Show4Meses => GetMonth(BirthDate) >= 4;
        public bool Show5Meses => GetMonth(BirthDate) >= 5;
        public bool Show6Meses => GetMonth(BirthDate) >= 6;
        public bool Show7Meses => GetMonth(BirthDate) >= 7 ;
        public bool Show9Meses => GetMonth(BirthDate) >= 9;
        public bool Show1Ano => GetMonth(BirthDate) >= 12;
        public bool Show15Meses => GetMonth(BirthDate) >= 15;
        public bool Show4Anos => GetMonth(BirthDate) >= 48;
        public bool Show5Anos => GetMonth(BirthDate) >= 60;
        public bool Show7Anos => GetMonth(BirthDate) >= 72;
        public bool Show9Anos => GetMonth(BirthDate) >= 108;


        // Propriedades derivadas
        [Ignore]
        public string Idade => CalcularIdadeCompleta(BirthDate);

        [Ignore]
        public bool IsOld => GetAge(BirthDate) >= 60;

        [Ignore]
        public bool IsBaby => GetAge(BirthDate) < 2;

        [Ignore]
        public bool IsChild => GetAge(BirthDate) <= 10;

        [Ignore]
        public bool IsDiabetesAndHypertension => HasDiabetes && HasHypertension;

        [Ignore]
        public string Endereco { get; set; } = "Sem endereço";

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

        private static int GetMonth(DateTime birthDate)
        {
            var today = DateTime.Today;

            int months = (today.Year - birthDate.Year) * 12 + today.Month - birthDate.Month;

            if (today.Day < birthDate.Day)
            {
                months--;
            }

            return Math.Max(months, 0); // Evita valor negativo
        }
    }
}
