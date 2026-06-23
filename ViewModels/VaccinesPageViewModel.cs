using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Application.Interfaces;
using ACS_View.Views;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class VaccinesPageViewModel : BaseViewModel
    {
        private readonly IPatientService _patientService;
        private readonly IVaccineService _vaccineService;
        private readonly IPopupService _popupService;
        private int? _loadedPatientId;
        private int _loadVersion;

        [ObservableProperty] private Patient? healthRecord;
        [ObservableProperty] private Vaccines? vaccines;
        [ObservableProperty] private bool hasPatient;
        [ObservableProperty] private ObservableCollection<VaccineSectionType> vaccineSections = [];

        public ICommand OpenVaccineInfo { get; set; }

        #region Cores por situação vacinal

        #region crianças
        public Color SituacaoRN => Vaccines?.SituacaoVacinal(
            Vaccines.BCG_Infantil,
            Vaccines.HepatitisBAoNascer_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao2Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Penta1_Infantil,
            Vaccines.VIP1_Infantil,
            Vaccines.Pneumo10_1_Infantil,
            Vaccines.VRH1_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao3Meses => Vaccines?.SituacaoVacinal(
            Vaccines.MeningoC1_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao4Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Penta2_Infantil,
            Vaccines.VIP2_Infantil,
            Vaccines.Pneumo10_2_Infantil,
            Vaccines.VRH2_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao5Meses => Vaccines?.SituacaoVacinal(
            Vaccines.MeningoC2_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao6Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Penta3_Infantil,
            Vaccines.VIP3_Infantil,
            Vaccines.Covid1_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao7Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Covid2_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao9Meses => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela1_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao12Meses => Vaccines?.SituacaoVacinal(
            Vaccines.Pneumo10_3_Infantil,
            Vaccines.MeningoC3_Infantil,
            Vaccines.TripliceViral_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao15Meses => Vaccines?.SituacaoVacinal(
            Vaccines.DTP1_Infantil,
            Vaccines.VIP4_Infantil,
            Vaccines.HepatiteA_Infantil,
            Vaccines.TetraViral_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao4Anos => Vaccines?.SituacaoVacinal(
            Vaccines.DTP2_Infantil,
            Vaccines.FebreAmarela2_Infantil,
            Vaccines.Varicela_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao5Anos => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela3_Infantil,
            Vaccines.Pneumo23_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao7Anos => Vaccines?.SituacaoVacinal(
            Vaccines.DT_Infantil
        ) ?? ThemeColors.ControlPressed;

        public Color Situacao9Anos => Vaccines?.SituacaoVacinal(
            Vaccines.HPV_Infantil
        ) ?? ThemeColors.ControlPressed;

        #endregion

        #region adolescentes
        public Color SituacaoHBAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.HepatiteB_Adolescente
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoDTAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.DT_Adolescente
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoFAAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela_Adolescente
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoTripliceViralAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.TripliceViral_Adolescente
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoHPVAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.HPV_Adolescente
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoACWYAdolescente => Vaccines?.SituacaoVacinal(
            Vaccines.ACWY_Adolescente
        ) ?? ThemeColors.ControlPressed;

        #endregion

        #region adultos

        public Color SituacaoHepatiteBAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.HepatiteB_Adulto
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoDTAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.dT_Adulto
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoFebreAmarelaAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela_Adulto
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoHPVAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.HPV_Adulto
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoTripliceViral1Adulto => Vaccines?.SituacaoVacinal(
            Vaccines.TripliceViral1_Adulto
            ) ?? ThemeColors.ControlPressed;

        public Color SituacaoTripliceViral2Adulto => Vaccines?.SituacaoVacinal(
            Vaccines.TripliceViral2_Adulto
            ) ?? ThemeColors.ControlPressed;

        public Color SituacaodTpaAdulto => Vaccines?.SituacaoVacinal(
            Vaccines.dTpa_Adulto
        ) ?? ThemeColors.ControlPressed;

        #endregion

        #region idosos

        public Color SituacaoHepatiteBIdoso => Vaccines?.SituacaoVacinal(
            Vaccines.HepatiteB_Idoso
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaodTIdoso => Vaccines?.SituacaoVacinal(
            Vaccines.dT_Idoso
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaoFebreAmarelaIdoso => Vaccines?.SituacaoVacinal(
            Vaccines.FebreAmarela_Idoso
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaodTpaIdoso => Vaccines?.SituacaoVacinal(
            Vaccines.dTpa_Idoso
        ) ?? ThemeColors.ControlPressed;

        #endregion

        #region gestantes

        public Color SituacaoHBGestante => Vaccines?.SituacaoVacinal(
            Vaccines.HepatiteB_Gestante
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaodTGestante => Vaccines?.SituacaoVacinal(
            Vaccines.dT_Gestante
        ) ?? ThemeColors.ControlPressed;

        public Color SituacaodTpaGestante => Vaccines?.SituacaoVacinal(
            Vaccines.dTpa_Gestante
        ) ?? ThemeColors.ControlPressed;

        #endregion

        #endregion

        public VaccinesPageViewModel(IPatientService patientService, IVaccineService vaccineService, IPopupService popupService)
        {
            _patientService = patientService;
            _vaccineService = vaccineService;
            _popupService = popupService;
            OpenVaccineInfo = new Command<string>(async (vaccine) => await OpenVaccineInfoCommand(vaccine));
        }

        public async Task LoadPatientAsync(int patientId)
        {
            if (patientId < 0)
            {
                await DisplayAlertAsync("Erro", "Paciente inválido.", "Voltar");
                return;
            }

            if (_loadedPatientId == patientId && HealthRecord is not null && Vaccines is not null)
            {
                return;
            }

            var loadVersion = Interlocked.Increment(ref _loadVersion);
            HealthRecord = null;
            Vaccines = null;
            HasPatient = false;
            IsLoading = true;

            try
            {
                var patient = await _patientService.GetPatientById(patientId);

                if (loadVersion != _loadVersion)
                {
                    return;
                }

                if (patient is null)
                {
                    await DisplayAlertAsync("Erro", "Paciente não encontrado.", "Voltar");
                    return;
                }

                var vaccinesRecord = await _vaccineService.GetVaccinesByIdAsync(patientId);

                if (loadVersion != _loadVersion)
                {
                    return;
                }

                if (vaccinesRecord is null)
                {
                    vaccinesRecord = new Vaccines
                    {
                        Id = patient.Id,
                        BirthDate = patient.BirthDate
                    };

                    await _vaccineService.AdicionarVacinasAsync(vaccinesRecord);
                }

                HealthRecord = patient;
                Vaccines = vaccinesRecord;
                HasPatient = true;
                _loadedPatientId = patientId;
            }
            finally
            {
                if (loadVersion == _loadVersion)
                {
                    IsLoading = false;
                }
            }
        }

        partial void OnVaccinesChanged(Vaccines? value)
        {
            RefreshVaccineSections(value);
            OnPropertyChanged(string.Empty);
        }

        private void RefreshVaccineSections(Vaccines? value)
        {
            VaccineSections.Clear();

            if (value is null)
            {
                return;
            }

            if (value.IsChild)
            {
                VaccineSections.Add(VaccineSectionType.Child);
            }

            if (value.IsYoung)
            {
                VaccineSections.Add(VaccineSectionType.Teen);
            }

            if (value.IsAdult)
            {
                VaccineSections.Add(VaccineSectionType.Adult);
            }

            if (value.IsElderly)
            {
                VaccineSections.Add(VaccineSectionType.Elderly);
            }

            if (value.IsPregnant)
            {
                VaccineSections.Add(VaccineSectionType.Pregnant);
            }
        }

        private bool GetVaccineStatus(string Vaccine)
        {
            return Vaccines?.GetVaccineStatus(Vaccine) ?? false;
        }

        private async Task OpenVaccineInfoCommand(string Vaccine)
        {
            try
            {
                if (Vaccines is null)
                {
                    return;
                }

                bool vaccineChecked = GetVaccineStatus(Vaccine);

                var popup = new VaccinesInfo(Vaccine, vaccineChecked);
                var status = await _popupService.ShowAsync<bool>(popup);

                if (status.WasDismissed) return;

                if (status.Result is bool vaccineStatus && vaccineStatus != vaccineChecked)
                {
                    await UpdateVaccine(Vaccine, vaccineStatus);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private async Task UpdateVaccine(string Vaccine, bool vaccineStatus)
        {
            try
            {
                if (_loadedPatientId is not int patientId)
                {
                    return;
                }

                var vaccineProperty = await _vaccineService.GetVaccinesByIdAsync(patientId);

                if (vaccineProperty is null)
                {
                    return;
                }

                if (vaccineProperty.GetVaccineStatus(Vaccine) != vaccineStatus)
                {
                    vaccineProperty.ChangeVaccineStatus(Vaccine);
                }

                await _vaccineService.AtualizarVacinasAsync(vaccineProperty);
                 
                Vaccines = vaccineProperty;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }
    }
}
