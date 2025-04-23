using SQLite;

namespace ACS_View.MVVM.Models
{
    public class Vaccines
    {
        [PrimaryKey]
        public string SusNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsPregnant { get; set; } = false;

        // Vacinas
        #region vacinas infantis
        public bool BCG_Infantil { get; set; } = false;
        public bool HepatitisBAoNascer_Infantil { get; set; } = false;
        public bool Penta1_Infantil { get; set; } = false;
        public bool VIP1_Infantil { get; set; } = false;
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

        public bool HepatiteB_Adolescente { get; set; } = false;
        public bool DT_Adolescente { get; set; } = false;
        public bool FebreAmarela_Adolescente { get; set; } = false;
        public bool TripliceViral_Adolescente { get; set; } = false;
        public bool HPV_Adolescente { get; set; } = false;
        public bool ACWY_Adolescente { get; set; } = false;

        #endregion

        #region vacinas adultos

        public bool HepatiteB_Adulto { get; set; } = false;
        public bool dT_Adulto { get; set; } = false;
        public bool FebreAmarela_Adulto { get; set; } = false;
        public bool HPV_Adulto { get; set; } = false;
        public bool TripliceViral1_Adulto { get; set; } = false;
        public bool TripliceViral2_Adulto { get; set; } = false;
        public bool dTpa_Adulto { get; set; } = false;

        #endregion

        #region vacinas idosos

        public bool HepatiteB_Idoso { get; set; } = false;
        public bool dT_Idoso { get; set; } = false;
        public bool FebreAmarela_Idoso { get; set; } = false;
        public bool dTpa_Idoso { get; set; } = false;

        #endregion

        #region vacinas gestantes

        public bool HepatiteB_Gestante { get; set; } = false;
        public bool dT_Gestante { get; set; } = false;
        public bool dTpa_Gestante { get; set; } = false;

        #endregion

        // Propriedades de exibição de vacinas
        #region propriedades de exibição

        #region crianças
        [Ignore]
        public bool ShowRN => GetMonth(BirthDate) >= 0 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show2Meses => GetMonth(BirthDate) >= 2 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show3Meses => GetMonth(BirthDate) >= 3 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show4Meses => GetMonth(BirthDate) >= 4 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show5Meses => GetMonth(BirthDate) >= 5 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show6Meses => GetMonth(BirthDate) >= 6 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show7Meses => GetMonth(BirthDate) >= 7 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show9Meses => GetMonth(BirthDate) >= 9 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show1Ano => GetMonth(BirthDate) >= 12 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show15Meses => GetMonth(BirthDate) >= 15 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show4Anos => GetMonth(BirthDate) >= 48 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show5Anos => GetMonth(BirthDate) >= 60 && GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool Show7Anos => GetMonth(BirthDate) >= 72
                              && GetMonth(BirthDate) <= 120
                              && !Penta1_Infantil
                              && !Penta2_Infantil
                              && !Penta3_Infantil
                              && !DTP1_Infantil
                              && !DTP2_Infantil;
        [Ignore]
        public bool Show9Anos => GetMonth(BirthDate) >= 108 && GetMonth(BirthDate) <= 120;
        #endregion

        #region adolescentes

        [Ignore]
        public bool ShowHB => GetMonth(BirthDate) > 120 && GetMonth(BirthDate) <= 180;
        [Ignore]
        public bool ShowDT => GetMonth(BirthDate) > 120 && GetMonth(BirthDate) <= 180;
        [Ignore]
        public bool ShowFebreAmarela => GetMonth(BirthDate) > 120 && GetMonth(BirthDate) <= 180 &&
        (
            (!FebreAmarela1_Infantil && !FebreAmarela2_Infantil && !FebreAmarela3_Infantil) ||  // nenhuma foi aplicada
            (!FebreAmarela1_Infantil || !FebreAmarela2_Infantil)                                // alguma das duas primeiras faltando
        );

        [Ignore]
        public bool ShowTripliceViral => GetMonth(BirthDate) > 120 && GetMonth(BirthDate) <= 180;
        [Ignore]
        public bool ShowHPV => GetMonth(BirthDate) > 120 && GetMonth(BirthDate) <= 180;
        [Ignore]
        public bool ShowACWY => GetMonth(BirthDate) > 120 && GetMonth(BirthDate) <= 180;

        #endregion

        #region adultos

        [Ignore]
        public bool ShowHBAdulto => GetMonth(BirthDate) > 180 && GetMonth(BirthDate) < 720;
        [Ignore]
        public bool ShowdTAdulto => GetMonth(BirthDate) > 180 && GetMonth(BirthDate) < 720;
        [Ignore]
        public bool ShowFebreAmarelaAdulto => GetMonth(BirthDate) > 180 && GetMonth(BirthDate) < 720;
        [Ignore]
        public bool ShowHPVAdulto => GetMonth(BirthDate) > 180 && GetMonth(BirthDate) < 720;
        [Ignore]
        public bool ShowTripliceViral1Adulto => GetMonth(BirthDate) > 180 && GetMonth(BirthDate) < 720;
        [Ignore]
        public bool ShowTripliceViral2Adulto => GetMonth(BirthDate) > 180 && GetMonth(BirthDate) < 720;
        [Ignore]
        public bool ShowdTpaAdulto => GetMonth(BirthDate) > 180 && GetMonth(BirthDate) < 720;

        #endregion

        #region idosos

        [Ignore]
        public bool ShowHBIdoso => GetMonth(BirthDate) >= 720;
        [Ignore]
        public bool ShowdTIdoso => GetMonth(BirthDate) >= 720;
        [Ignore]
        public bool ShowFebreAmarelaIdoso => GetMonth(BirthDate) >= 720;
        [Ignore]
        public bool ShowdTpaIdoso => GetMonth(BirthDate) >= 720;

        #endregion

        #region gestantes

        [Ignore]
        public bool ShowHBGestante => IsPregnant;
        [Ignore]
        public bool ShowdTGestante => IsPregnant;
        [Ignore]
        public bool ShowdTpaGestante => IsPregnant;

        #endregion

        [Ignore]
        public bool IsChild => GetMonth(BirthDate) <= 120;
        [Ignore]
        public bool IsYoung => GetMonth(BirthDate) > 120 && GetMonth(BirthDate) <= 180;
        [Ignore]
        public bool IsAdult => GetMonth(BirthDate) > 180 && GetMonth(BirthDate) < 720;
        [Ignore]
        public bool IsElderly => GetMonth(BirthDate) >= 720;
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
