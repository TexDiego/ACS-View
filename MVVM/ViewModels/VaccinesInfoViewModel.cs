namespace ACS_View.MVVM.ViewModels
{
    public class VaccinesInfoViewModel
    {
        private Dictionary<string, string> _DiseasesAvoided = new Dictionary<string, string>
        {
            { "BCG", "Tuberculose" },
            { "Hepatite B", "Hepatite B" },
            { "Penta", "Difteria, Tétano, Coqueluche, Hepatite B e infecções causadas pelo Haemophilus influenzae B" },
            { "VIP", "Poliomelite" },
            { "Pneumo 10", "Infecções invasivas (como meningite e pneumonia) e otite média média aguda, causadas pelos 10 sorotipos de Streptococus pneumoniae" },
            { "VRH", "Diarreia por rotavírus (Gastroenterites)" },
            { "Meningo C", "Doença invasiva causada pela Neisseria meningitidis do sorogrupo C" },
            { "Covid 19", "Proteção contra as formas graves e complicações pela covid-19" },
            { "Febre Amarela", "Febre amarela" },
            { "Triplice Viral", "Sarampo, caxumba e rubéola" },
            { "DTP", "Difteria, tétano e coqueluche" },
            { "Hepatite A", "Hepatite A" },
            { "Tetra Viral", "Sarampo, caxumba, rubéola e varicela" },
            { "Varicela", "Varicela" },
            { "Pneumo 23", "Para a proteção contra infecções invasivas pela bactéria pneumococo" },
            { "DT", "Difteria e Tétano" },
            { "HPV", "Proteção contra Papilomavírus Humano 6, 11, 16 e 18" },
            { "ACWY", "Proteção contra as meningites causadas pelos sorogrupos A, C, W e Y da Neisseria meningitidis" },
            { "DTPA", "Proteção contra Difteria, Tétano e Coqueluche em adultos" }
        };

        private Dictionary<string, string> _Diseases = new Dictionary<string, string>
        {
            { "BCG_Infantil", "BCG" },
            { "HB", "Hepatite B" },
            { "HB2", "Hepatite B" },
            { "HB3", "Hepatite B" },
            { "HB4", "Hepatite B" },
            { "HB5", "Hepatite B" },
            { "Penta1", "Penta" },
            { "Penta2", "Penta" },
            { "Penta3", "Penta" },
            { "VIP1", "VIP" },
            { "VIP2", "VIP" },
            { "VIP3", "VIP" },
            { "VIP4", "VIP" },
            { "Pneumo10-1", "Pneumo 10" },
            { "Pneumo10-2", "Pneumo 10" },
            { "Pneumo10-3", "Pneumo 10" },
            { "VRH1", "VRH" },
            { "VRH2", "VRH" },
            { "MeningoC1", "Meningo C" },
            { "MeningoC2", "Meningo C" },
            { "MeningoC3", "Meningo C" },
            { "Covid1", "Covid 19" },
            { "Covid2", "Covid 19" },
            { "FA1", "Febre Amarela" },
            { "FA2", "Febre Amarela" },
            { "FA3", "Febre Amarela" },
            { "FA4", "Febre Amarela" },
            { "FA5", "Febre Amarela" },
            { "FA6", "Febre Amarela" },
            { "TripliceViral1", "Triplice Viral" },
            { "TripliceViral2", "Triplice Viral" },
            { "TripliceViral3", "Triplice Viral" },
            { "TripliceViral4", "Triplice Viral" },
            { "DTP1", "DTP" },
            { "DTP2", "DTP" },
            { "HA", "Hepatite A" },
            { "TetraViral", "Tetra Viral" },
            { "Varicela", "Varicela" },
            { "Pneumo23", "Pneumo 23" },
            { "DT", "DT" },
            { "DT2", "DT" },
            { "DT3", "DT" },
            { "DT4", "DT" },
            { "DT5", "DT" },
            { "HPV", "HPV" },
            { "HPV2", "HPV" },
            { "HPV3", "HPV" },
            { "ACWY", "ACWY" },
            { "DTPA", "DTPA" },
            { "DTPA2", "DTPA" },
            { "DTPA3", "DTPA" }
        };

        private readonly string _VaccineName;

        public string DiseaseDescription { get; private set; }
        public string VaccineName { get; private set; }

        public VaccinesInfoViewModel(string vaccineName)
        {
            _VaccineName = vaccineName;

            VaccineName = _Diseases.TryGetValue(_VaccineName, out var name) ? name : "Informação não disponível";
            DiseaseDescription = _DiseasesAvoided.TryGetValue(_Diseases[_VaccineName], out var desc) ? desc : "Informação não disponível";
        }
    }
}
