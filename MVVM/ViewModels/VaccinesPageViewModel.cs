using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;

namespace ACS_View.MVVM.ViewModels
{
    internal class VaccinesPageViewModel
    {
        #region Vacinas
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
        #endregion

        private HealthRecordService _healthRecordService;
        public HealthRecord HealthRecord { get; set ; }

        public VaccinesPageViewModel() { }
        public VaccinesPageViewModel(HealthRecordService healthRecordService, string sus)
        {
            _healthRecordService = healthRecordService;
            HealthRecord = GetRecordBySusAsync(sus).Result;
        }

        private Task<HealthRecord> GetRecordBySusAsync(string sus)
        {
            return _healthRecordService.GetRecordBySusAsync(sus);
        }
    }
}
