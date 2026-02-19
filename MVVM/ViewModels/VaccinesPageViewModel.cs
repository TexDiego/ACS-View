using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class VaccinesPageViewModel : BaseViewModel
    {
        private readonly IPatientService _patientService = App.ServiceProvider.GetRequiredService<IPatientService>();
        private readonly IVaccineService _vaccineService = App.ServiceProvider.GetRequiredService<IVaccineService>();
        public Patient HealthRecord { get; set; }

        [ObservableProperty] private Vaccines vaccines;

        private readonly int id;

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

        public VaccinesPageViewModel(int id)
        {
            this.id = id;
            OpenVaccineInfo = new Command<string>(async (vaccine) => await OpenVaccineInfoCommand(vaccine));
        }

        public async Task InitializeAsync()
        {
            Vaccines = await _vaccineService.GetVaccinesByIdAsync(id);
            HealthRecord = await _patientService.GetPatientById(id);
        }

        private bool GetVaccineStatus(string Vaccine)
        {
            Console.WriteLine("Vacina: " + Vaccine + ", status: " + Vaccines?.GetVaccineStatus(Vaccine));
            return Vaccines?.GetVaccineStatus(Vaccine) ?? false;
        }

        private async Task OpenVaccineInfoCommand(string Vaccine)
        {
            try
            {
                bool vaccineChecked = GetVaccineStatus(Vaccine);
                Console.WriteLine($"Vacina: {Vaccine}, Status: {vaccineChecked}");

                var popup = new VaccinesInfo(Vaccine, vaccineChecked);
                var status = await Shell.Current.ShowPopupAsync(popup);

                if (status is bool vaccineStatus && vaccineStatus != vaccineChecked)
                {
                    await UpdateVaccine(Vaccine);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.ShowPopupAsync(
                    new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async Task UpdateVaccine(string Vaccine)
        {
            try
            {
                var vaccineProperty = await _vaccineService.GetVaccinesByIdAsync(id);

                vaccineProperty?.ChangeVaccineStatus(Vaccine);

                await _vaccineService.AtualizarVacinasAsync(vaccineProperty);
                 
                Vaccines = vaccineProperty;
            }
            catch (Exception ex)
            {
                await Shell.Current.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }
    }
}
