using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ACS_View.MVVM.ViewModels
{
    public class VaccinesPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private HealthRecordService _healthRecordService;
        private VaccineService _vaccineService;
        public HealthRecord HealthRecord { get; set; }
        public Vaccines Vaccines { get; set; }

        // Cores por faixa etária
        public Color SituacaoRN => Vaccines?.SituacaoVacinal(
            Vaccines.BCG_Infantil,
            Vaccines.HepatitisBAoNascer_Infantil
        ) ?? Colors.Red;

        public Color Situacao2Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Penta1_Infantil,
            Vaccines.VIP1_Infantil,
            Vaccines.Pneumo10_1_Infantil,
            Vaccines.VRH1_Infantil,
            Vaccines.MeningoC1_Infantil
        ) ?? Colors.Grey;

        public Color Situacao3Meses => Vaccines?.SituacaoVacinal(
            Vaccines.MeningoC2_Infantil
        ) ?? Colors.Grey;

        public Color Situacao4Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Penta2_Infantil,
            Vaccines.VIP2_Infantil,
            Vaccines.Pneumo10_2_Infantil,
            Vaccines.VRH2_Infantil
        ) ?? Colors.Grey;

        public Color Situacao5Meses => Vaccines?.SituacaoVacinal(
            Vaccines.MeningoC2_Infantil
        ) ?? Colors.Grey;

        public Color Situacao6Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Penta3_Infantil,
            Vaccines.VIP3_Infantil,
            Vaccines.Covid1_Infantil
        ) ?? Colors.Grey;

        public Color Situacao7Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Covid2_Infantil
        ) ?? Colors.Grey;

        public Color Situacao9Meses => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela1_Infantil
        ) ?? Colors.Grey;

        public Color Situacao12Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Pneumo10_3_Infantil,
            Vaccines.MeningoC3_Infantil,
            Vaccines.TripliceViral_Infantil
        ) ?? Colors.Grey;

        public Color Situacao15Meses => Vaccines?.SituacaoVacinal(
            Vaccines.DTP1_Infantil,
            Vaccines.VIP4_Infantil,
            Vaccines.HepatiteA_Infantil,
            Vaccines.TetraViral_Infantil
        ) ?? Colors.Grey;

        public Color Situacao4Anos => Vaccines?.SituacaoVacinal(
            Vaccines.DTP2_Infantil,
            Vaccines.FebreAmarela2_Infantil,
            Vaccines.Varicela_Infantil
        ) ?? Colors.Grey;

        public Color Situacao5Anos => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela3_Infantil,
            Vaccines.Pneumo23_Infantil
        ) ?? Colors.Grey;

        public Color Situacao7Anos => Vaccines?.SituacaoVacinal(
            Vaccines.DT_Infantil
        ) ?? Colors.Grey;

        public Color Situacao9Anos => Vaccines?.SituacaoVacinal(
            Vaccines.HPV_Infantil
        ) ?? Colors.Grey;

        public VaccinesPageViewModel() { }

        public VaccinesPageViewModel(HealthRecordService healthRecordService, VaccineService vaccineService, string sus)
        {
            _healthRecordService = healthRecordService;
            _vaccineService = vaccineService;
            LoadDataAsync(sus);
        }

        private async void LoadDataAsync(string sus)
        {
            HealthRecord = await _healthRecordService.GetRecordBySusAsync(sus);
            OnPropertyChanged(nameof(HealthRecord));
            OnPropertyChanged(nameof(SituacaoRN));
            OnPropertyChanged(nameof(Situacao2Meses));
            OnPropertyChanged(nameof(Situacao3Meses));
            OnPropertyChanged(nameof(Situacao4Meses));
            OnPropertyChanged(nameof(Situacao5Meses));
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
