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
        public bool IsChild => GetMonth(BirthDate) <= 720;
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
        public static int GetMonth(DateTime birthDate)
        {
            var today = DateTime.Today;

            int months = (today.Year - birthDate.Year) * 12 + today.Month - birthDate.Month;

            if (today.Day < birthDate.Day)
            {
                months--;
            }

            return Math.Max(months, 0); // Evita valor negativo
        }

        // Métodos para obter e alterar o status da vacina
        public void ChangeVaccineStatus(string vaccineName)
        {
            switch (vaccineName)
            {
                case "BCG_Infantil":
                    BCG_Infantil = !BCG_Infantil;
                    break;
                case "HB":
                    HepatitisBAoNascer_Infantil = !HepatitisBAoNascer_Infantil;
                    break;
                case "HB2":
                    HepatiteB_Adolescente = !HepatiteB_Adolescente;
                    break;
                case "Penta1":
                    Penta1_Infantil = !Penta1_Infantil;
                    break;
                case "VIP1":
                    VIP1_Infantil = !VIP1_Infantil;
                    break;
                case "Pneumo10-1":
                    Pneumo10_1_Infantil = !Pneumo10_1_Infantil;
                    break;
                case "VRH1":
                    VRH1_Infantil = !VRH1_Infantil;
                    break;
                case "MeningoC1":
                    MeningoC1_Infantil = !MeningoC1_Infantil;
                    break;
                case "Penta2":
                    Penta2_Infantil = !Penta2_Infantil;
                    break;
                case "VIP2":
                    VIP2_Infantil = !VIP2_Infantil;
                    break;
                case "Pneumo10-2":
                    Pneumo10_2_Infantil = !Pneumo10_2_Infantil;
                    break;
                case "VRH2":
                    VRH2_Infantil = !VRH2_Infantil;
                    break;
                case "MeningoC2":
                    MeningoC2_Infantil = !MeningoC2_Infantil;
                    break;
                case "Penta3":
                    Penta3_Infantil = !Penta3_Infantil;
                    break;
                case "VIP3":
                    VIP3_Infantil = !VIP3_Infantil;
                    break;
                case "Covid1":
                    Covid1_Infantil = !Covid1_Infantil;
                    break;
                case "Covid2":
                    Covid2_Infantil = !Covid2_Infantil;
                    break;
                case "FA1":
                    FebreAmarela1_Infantil = !FebreAmarela1_Infantil;
                    break;
                case "Pneumo10-3":
                    Pneumo10_3_Infantil = !Pneumo10_3_Infantil;
                    break;
                case "MeningoC3":
                    MeningoC3_Infantil = !MeningoC3_Infantil;
                    break;
                case "TripliceViral1":
                    TripliceViral_Infantil = !TripliceViral_Infantil;
                    break;
                case "DTP1":
                    DTP1_Infantil = !DTP1_Infantil;
                    break;
                case "VIP4":
                    VIP4_Infantil = !VIP4_Infantil;
                    break;
                case "HA":
                    HepatiteA_Infantil = !HepatiteA_Infantil;
                    break;
                case "TetraViral":
                    TetraViral_Infantil = !TetraViral_Infantil;
                    break;
                case "DTP2":
                    DTP2_Infantil = !DTP2_Infantil;
                    break;
                case "FA2":
                    FebreAmarela2_Infantil = !FebreAmarela2_Infantil;
                    break;
                case "Varicela":
                    Varicela_Infantil = !Varicela_Infantil;
                    break;
                case "FA3":
                    FebreAmarela3_Infantil = !FebreAmarela3_Infantil;
                    break;
                case "Pneumo23":
                    Pneumo23_Infantil = !Pneumo23_Infantil;
                    break;
                case "DT":
                    DT_Infantil = !DT_Infantil;
                    break;
                case "DT2":
                    DT_Adolescente = !DT_Adolescente;
                    break;
                case "HPV":
                    HPV_Infantil = !HPV_Infantil;
                    break;
                case "FA4":
                    FebreAmarela_Adolescente = !FebreAmarela_Adolescente;
                    break;
                case "TripliceViral2":
                    TripliceViral_Adolescente = !TripliceViral_Adolescente;
                    break;
                case "HPV2":
                    HPV_Adolescente = !HPV_Adolescente;
                    break;
                case "ACWY":
                    ACWY_Adolescente = !ACWY_Adolescente;
                    break;
                case "HB3":
                    HepatiteB_Adulto = !HepatiteB_Adulto;
                    break;
                case "DT3":
                    dT_Adulto = !dT_Adulto;
                    break;
                case "FA5":
                    FebreAmarela_Adulto = !FebreAmarela_Adulto;
                    break;
                case "HPV3":
                    HPV_Adulto = !HPV_Adulto;
                    break;
                case "TripliceViral3":
                    TripliceViral1_Adulto = !TripliceViral1_Adulto;
                    break;
                case "TripliceViral4":
                    TripliceViral2_Adulto = !TripliceViral2_Adulto;
                    break;
                case "DTPA":
                    dTpa_Adulto = !dTpa_Adulto;
                    break;
                case "HB4":
                    HepatiteB_Idoso = !HepatiteB_Idoso;
                    break;
                case "DT4":
                    dT_Idoso = !dT_Idoso;
                    break;
                case "FA6":
                    FebreAmarela_Idoso = !FebreAmarela_Idoso;
                    break;
                case "DTPA2":
                    dTpa_Idoso = !dTpa_Idoso;
                    break;
                case "HB5":
                    HepatiteB_Gestante = !HepatiteB_Gestante;
                    break;
                case "DT5":
                    dT_Gestante = !dT_Gestante;
                    break;
                case "DTPA3":
                    dTpa_Gestante = !dTpa_Gestante;
                    break;
            }
        }

        public bool GetVaccineStatus(string vaccineName)
        {
            return vaccineName switch
            {
                "BCG_Infantil" => BCG_Infantil,
                "HB" => HepatitisBAoNascer_Infantil,
                "HB2" => HepatiteB_Adolescente,
                "Penta1" => Penta1_Infantil,
                "VIP1" => VIP1_Infantil,
                "Pneumo10-1" => Pneumo10_1_Infantil,
                "VRH1" => VRH1_Infantil,
                "MeningoC1" => MeningoC1_Infantil,
                "Penta2" => Penta2_Infantil,
                "VIP2" => VIP2_Infantil,
                "Pneumo10-2" => Pneumo10_2_Infantil,
                "VRH2" => VRH2_Infantil,
                "MeningoC2" => MeningoC2_Infantil,
                "Penta3" => Penta3_Infantil,
                "VIP3" => VIP3_Infantil,
                "Covid1" => Covid1_Infantil,
                "Covid2" => Covid2_Infantil,
                "FA1" => FebreAmarela1_Infantil,
                "Pneumo10-3" => Pneumo10_3_Infantil,
                "MeningoC3" => MeningoC3_Infantil,
                "TripliceViral1" => TripliceViral_Infantil,
                "DTP1" => DTP1_Infantil,
                "VIP4" => VIP4_Infantil,
                "HA" => HepatiteA_Infantil,
                "TetraViral" => TetraViral_Infantil,
                "DTP2" => DTP2_Infantil,
                "FA2" => FebreAmarela2_Infantil,
                "Varicela" => Varicela_Infantil,
                "FA3" => FebreAmarela3_Infantil,
                "Pneumo23" => Pneumo23_Infantil,
                "DT" => DT_Infantil,
                "DT2" => DT_Adolescente,
                "HPV" => HPV_Infantil,
                "FA4" => FebreAmarela_Adolescente,
                "TripliceViral2" => TripliceViral_Adolescente,
                "HPV2" => HPV_Adolescente,
                "ACWY" => ACWY_Adolescente,
                "HB3" => HepatiteB_Adulto,
                "DT3" => dT_Adulto,
                "FA5" => FebreAmarela_Adulto,
                "HPV3" => HPV_Adulto,
                "TripliceViral3" => TripliceViral1_Adulto,
                "TripliceViral4" => TripliceViral2_Adulto,
                "DTPA" => dTpa_Adulto,
                "HB4" => HepatiteB_Idoso,
                "DT4" => dT_Idoso,
                "FA6" => FebreAmarela_Idoso,
                "DTPA2" => dTpa_Idoso,
                "HB5" => HepatiteB_Gestante,
                "DT5" => dT_Gestante,
                "DTPA3" => dTpa_Gestante,
                _ => false
            };
        }
    }
}