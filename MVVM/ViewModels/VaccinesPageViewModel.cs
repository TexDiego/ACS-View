using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public class VaccinesPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private HealthRecordService _healthRecordService;
        private VaccineService _vaccineService;
        public HealthRecord HealthRecord { get; set; }
        public Vaccines Vaccines { get; set; }
        private readonly string _susNumber;

        public ICommand OpenVaccineInfo { get; set; }

        #region Cores por situação vacinal

        #region crianças
        public Color SituacaoRN => Vaccines?.SituacaoVacinal(
            Vaccines.BCG_Infantil,
            Vaccines.HepatitisBAoNascer_Infantil
        ) ?? Colors.Grey;

        public Color Situacao2Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Penta1_Infantil,
            Vaccines.VIP1_Infantil,
            Vaccines.Pneumo10_1_Infantil,
            Vaccines.VRH1_Infantil
        ) ?? Colors.Grey;

        public Color Situacao3Meses => Vaccines?.SituacaoVacinal(
            Vaccines.MeningoC1_Infantil
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

        #endregion

        #region adolescentes
        public Color SituacaoHBAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.HepatiteB_Adolescente
        ) ?? Colors.Grey;

        public Color SituacaoDTAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.DT_Adolescente
        ) ?? Colors.Grey;

        public Color SituacaoFAAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela_Adolescente
        ) ?? Colors.Grey;

        public Color SituacaoTripliceViralAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.TripliceViral_Adolescente
        ) ?? Colors.Grey;

        public Color SituacaoHPVAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.HPV_Adolescente
        ) ?? Colors.Grey;

        public Color SituacaoACWYAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.ACWY_Adolescente
        ) ?? Colors.Grey;

        #endregion

        #region adultos

        public Color SituacaoHepatiteBAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.HepatiteB_Adulto
        ) ?? Colors.Grey;

        public Color SituacaoDTAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.dT_Adulto
        ) ?? Colors.Grey;

        public Color SituacaoFebreAmarelaAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela_Adulto
        ) ?? Colors.Grey;

        public Color SituacaoHPVAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.HPV_Adulto
        ) ?? Colors.Grey;

        public Color SituacaoTripliceViral1Adulto => Vaccines?.SituacaoVacinal(
            Vaccines.TripliceViral1_Adulto
            ) ?? Colors.Grey;

        public Color SituacaoTripliceViral2Adulto => Vaccines?.SituacaoVacinal(
            Vaccines.TripliceViral2_Adulto
            ) ?? Colors.Grey;

        public Color SituacaodTpaAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.dTpa_Adulto
        ) ?? Colors.Grey;

        #endregion

        #region idosos

        public Color SituacaoHepatiteBIdoso => Vaccines?.SituacaoVacinal(
            Vaccines.HepatiteB_Idoso
        ) ?? Colors.Grey;

        public Color SituacaodTIdoso => Vaccines?.SituacaoVacinal(
            Vaccines.dT_Idoso
        ) ?? Colors.Grey;

        public Color SituacaoFebreAmarelaIdoso => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela_Idoso
        ) ?? Colors.Grey;

        public Color SituacaodTpaIdoso => Vaccines?.SituacaoVacinal(
            Vaccines.dTpa_Idoso
        ) ?? Colors.Grey;

        #endregion

        #region gestantes

        public Color SituacaoHBGestante => Vaccines?.SituacaoVacinal(
            Vaccines.HepatiteB_Gestante
        ) ?? Colors.Grey;

        public Color SituacaodTGestante => Vaccines?.SituacaoVacinal(
            Vaccines.dT_Gestante
        ) ?? Colors.Grey;

        public Color SituacaodTpaGestante => Vaccines?.SituacaoVacinal(
            Vaccines.dTpa_Gestante
        ) ?? Colors.Grey;

        #endregion

        #endregion

        public VaccinesPageViewModel() { }

        public VaccinesPageViewModel(HealthRecordService healthRecordService, VaccineService vaccineService, string sus)
        {
            _healthRecordService = healthRecordService;
            _vaccineService = vaccineService;
            _susNumber = sus;
            OpenVaccineInfo = new Command<string>(async (vaccine) => await OpenVaccineInfoCommand(vaccine));
        }

        public async Task InitializeAsync()
        {
            Vaccines = await _vaccineService.GetVaccinesBySusAsync(_susNumber);
            HealthRecord = await _healthRecordService.GetRecordBySusAsync(_susNumber);
            NotifyAllSituacaoProperties();
            OnPropertyChanged(nameof(HealthRecord));
        }

        private bool GetVaccineStatus(string vaccine)
        {
            Console.WriteLine("Vacina: " + vaccine + ", status: " + Vaccines?.GetVaccineStatus(vaccine));
            return Vaccines?.GetVaccineStatus(vaccine) ?? false;
        }

        private async Task OpenVaccineInfoCommand(string vaccine)
        {
            try
            {
                bool vaccineChecked = GetVaccineStatus(vaccine);
                Console.WriteLine($"Vacina: {vaccine}, Status: {vaccineChecked}");

                var popup = new VaccinesInfo(vaccine, vaccineChecked);
                var status = await Application.Current.MainPage.ShowPopupAsync(popup);

                if (status is bool vaccineStatus && vaccineStatus != vaccineChecked)
                {
                    await UpdateVaccine(vaccine);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async Task UpdateVaccine(string vaccine)
        {
            try
            {
                var vaccineProperty = await _vaccineService.GetVaccinesBySusAsync(_susNumber);

                vaccineProperty?.ChangeVaccineStatus(vaccine);

                await _vaccineService.AtualizarVacinasAsync(vaccineProperty);
                 
                Vaccines = vaccineProperty;
                OnPropertyChanged(nameof(Vaccines));
                NotifyAllSituacaoProperties();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void NotifyAllSituacaoProperties()
        {
            OnPropertyChanged(nameof(SituacaoRN));
            OnPropertyChanged(nameof(Situacao2Meses));
            OnPropertyChanged(nameof(Situacao3Meses));
            OnPropertyChanged(nameof(Situacao4Meses));
            OnPropertyChanged(nameof(Situacao5Meses));
            OnPropertyChanged(nameof(Situacao6Meses));
            OnPropertyChanged(nameof(Situacao7Meses));
            OnPropertyChanged(nameof(Situacao9Meses));
            OnPropertyChanged(nameof(Situacao12Meses));
            OnPropertyChanged(nameof(Situacao15Meses));
            OnPropertyChanged(nameof(Situacao4Anos));
            OnPropertyChanged(nameof(Situacao5Anos));
            OnPropertyChanged(nameof(Situacao7Anos));
            OnPropertyChanged(nameof(Situacao9Anos));
            OnPropertyChanged(nameof(SituacaoHBAdolescente));
            OnPropertyChanged(nameof(SituacaoDTAdolescente));
            OnPropertyChanged(nameof(SituacaoFAAdolescente));
            OnPropertyChanged(nameof(SituacaoTripliceViralAdolescente));
            OnPropertyChanged(nameof(SituacaoHPVAdolescente));
            OnPropertyChanged(nameof(SituacaoACWYAdolescente));
            OnPropertyChanged(nameof(SituacaoHepatiteBAdulto));
            OnPropertyChanged(nameof(SituacaoDTAdulto));
            OnPropertyChanged(nameof(SituacaoFebreAmarelaAdulto));
            OnPropertyChanged(nameof(SituacaoHPVAdulto));
            OnPropertyChanged(nameof(SituacaoTripliceViral1Adulto));
            OnPropertyChanged(nameof(SituacaoTripliceViral2Adulto));
            OnPropertyChanged(nameof(SituacaodTpaAdulto));
            OnPropertyChanged(nameof(SituacaoHepatiteBIdoso));
            OnPropertyChanged(nameof(SituacaodTIdoso));
            OnPropertyChanged(nameof(SituacaoFebreAmarelaIdoso));
            OnPropertyChanged(nameof(SituacaodTpaIdoso));
            OnPropertyChanged(nameof(SituacaoHBGestante));
            OnPropertyChanged(nameof(SituacaodTGestante));
            OnPropertyChanged(nameof(SituacaodTpaGestante));
        }
    }
}
