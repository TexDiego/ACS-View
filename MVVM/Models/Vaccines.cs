using SQLite;

namespace ACS_View.MVVM.Models
{
    public class Vaccines
    {
        [PrimaryKey]
        public string SusNumber { get; set; }
        public DateTime BirthDate { get; set; }

        // Vacinas
        #region vacinas infantis
        public bool BCG_Infantil { get; set; } = false;
        public bool HepatitisBAoNascer_Infantil { get; set; } = false;
        public bool Penta1_Infantil { get; set; } = false;
        public bool VIP1_Infantil { get; set; } = true;
        public bool Pneumo10_1_Infantil { get; set; } = false;
        public bool VRH1_Infantil { get; set; } = false;
        public bool MeningoC1_Infantil { get; set; } = false;
        public bool Penta2_Infantil { get; set; } = false;
        public bool VIP2_Infantil { get; set; } = false;
        public bool Pneumo10_2_Infantil { get; set; } = false;
        public bool VRH2_Infantil { get; set; } = false;
        public bool MeningoC2_Infantil { get; set; } = false;
        public bool Penta3_Infantil { get; set; } = false;
        public bool VIP3_Infantil { get; set; } = false;
        public bool Covid1_Infantil { get; set; } = false;
        public bool Covid2_Infantil { get; set; } = false;
        public bool FebreAmarela1_Infantil { get; set; } = false;
        public bool Pneumo10_3_Infantil { get; set; } = false;
        public bool MeningoC3_Infantil { get; set; } = false;
        public bool TripliceViral_Infantil { get; set; } = false;
        public bool DTP1_Infantil { get; set; } = false;
        public bool VIP4_Infantil { get; set; } = false;
        public bool HepatiteA_Infantil { get; set; } = false;
        public bool TetraViral_Infantil { get; set; } = false;
        public bool DTP2_Infantil { get; set; } = false;
        public bool FebreAmarela2_Infantil { get; set; } = false;
        public bool Varicela_Infantil { get; set; } = false;
        public bool FebreAmarela3_Infantil { get; set; } = false;
        public bool Pneumo23_Infantil { get; set; } = false;
        public bool DT_Infantil { get; set; } = false;
        public bool HPV_Infantil { get; set; } = false;
        #endregion

        #region vacinas adolescentes


        #endregion

        #region vacinas adultos


        #endregion

        #region vacinas idosos


        #endregion

        #region vacinas gestantes


        #endregion

        // Propriedades de exibição de vacinas
        #region propriedades de idade
        public bool ShowRN => GetMonth(BirthDate) >= 0;
        public bool Show2Meses => GetMonth(BirthDate) >= 2;
        public bool Show3Meses => GetMonth(BirthDate) >= 3;
        public bool Show4Meses => GetMonth(BirthDate) >= 4;
        public bool Show5Meses => GetMonth(BirthDate) >= 5;
        public bool Show6Meses => GetMonth(BirthDate) >= 6;
        public bool Show7Meses => GetMonth(BirthDate) >= 7;
        public bool Show9Meses => GetMonth(BirthDate) >= 9;
        public bool Show1Ano => GetMonth(BirthDate) >= 12;
        public bool Show15Meses => GetMonth(BirthDate) >= 15;
        public bool Show4Anos => GetMonth(BirthDate) >= 48;
        public bool Show5Anos => GetMonth(BirthDate) >= 60;
        public bool Show7Anos => GetMonth(BirthDate) >= 72;
        public bool Show9Anos => GetMonth(BirthDate) >= 108;
        #endregion

        // Propriedade de cor para vacinas
        public Color SituacaoVacinal(params bool[] vacinas)
        {
            if (vacinas.All(v => v)) return Colors.Green;
            if (vacinas.All(v => !v)) return Colors.Red;
            return Colors.Orange;
        }

        // Método para calcular a idade em meses
        public int GetMonth(DateTime birthDate)
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
