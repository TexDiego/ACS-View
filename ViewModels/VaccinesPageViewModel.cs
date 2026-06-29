using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
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
        [ObservableProperty] private VaccineSchedulePresentation? vaccines;
        [ObservableProperty] private bool hasPatient;
        [ObservableProperty] private ObservableCollection<VaccineSectionType> vaccineSections = [];

        public ICommand OpenVaccineInfo { get; }

        #region Cores por situação vacinal

        public Color SituacaoRN => SituacaoVacinal(VaccineDoseKeys.BcgInfantil, VaccineDoseKeys.HepatiteBNascimento);
        public Color Situacao2Meses => SituacaoVacinal(VaccineDoseKeys.Penta1, VaccineDoseKeys.Vip1, VaccineDoseKeys.Pneumo10_1, VaccineDoseKeys.Vrh1);
        public Color Situacao3Meses => SituacaoVacinal(VaccineDoseKeys.MeningoC1);
        public Color Situacao4Meses => SituacaoVacinal(VaccineDoseKeys.Penta2, VaccineDoseKeys.Vip2, VaccineDoseKeys.Pneumo10_2, VaccineDoseKeys.Vrh2);
        public Color Situacao5Meses => SituacaoVacinal(VaccineDoseKeys.MeningoC2);
        public Color Situacao6Meses => SituacaoVacinal(VaccineDoseKeys.Penta3, VaccineDoseKeys.Vip3, VaccineDoseKeys.Covid1);
        public Color Situacao7Meses => SituacaoVacinal(VaccineDoseKeys.Covid2);
        public Color Situacao9Meses => SituacaoVacinal(VaccineDoseKeys.FebreAmarela1);
        public Color Situacao12Meses => SituacaoVacinal(VaccineDoseKeys.Pneumo10_3, VaccineDoseKeys.MeningoC3, VaccineDoseKeys.TripliceViralInfantil);
        public Color Situacao15Meses => SituacaoVacinal(VaccineDoseKeys.Dtp1, VaccineDoseKeys.Vip4, VaccineDoseKeys.HepatiteA, VaccineDoseKeys.TetraViral);
        public Color Situacao4Anos => SituacaoVacinal(VaccineDoseKeys.Dtp2, VaccineDoseKeys.FebreAmarela2, VaccineDoseKeys.Varicela);
        public Color Situacao5Anos => SituacaoVacinal(VaccineDoseKeys.FebreAmarela3, VaccineDoseKeys.Pneumo23);
        public Color Situacao7Anos => SituacaoVacinal(VaccineDoseKeys.DtInfantil);
        public Color Situacao9Anos => SituacaoVacinal(VaccineDoseKeys.HpvInfantil);

        public Color SituacaoHBAdolescente => SituacaoVacinal(VaccineDoseKeys.HepatiteBAdolescente);
        public Color SituacaoDTAdolescente => SituacaoVacinal(VaccineDoseKeys.DtAdolescente);
        public Color SituacaoFAAdolescente => SituacaoVacinal(VaccineDoseKeys.FebreAmarelaAdolescente);
        public Color SituacaoTripliceViralAdolescente => SituacaoVacinal(VaccineDoseKeys.TripliceViralAdolescente);
        public Color SituacaoHPVAdolescente => SituacaoVacinal(VaccineDoseKeys.HpvAdolescente);
        public Color SituacaoACWYAdolescente => SituacaoVacinal(VaccineDoseKeys.Acwy);

        public Color SituacaoHepatiteBAdulto => SituacaoVacinal(VaccineDoseKeys.HepatiteBAdulto);
        public Color SituacaoDTAdulto => SituacaoVacinal(VaccineDoseKeys.DtAdulto);
        public Color SituacaoFebreAmarelaAdulto => SituacaoVacinal(VaccineDoseKeys.FebreAmarelaAdulto);
        public Color SituacaoHPVAdulto => SituacaoVacinal(VaccineDoseKeys.HpvAdulto);
        public Color SituacaoTripliceViral1Adulto => SituacaoVacinal(VaccineDoseKeys.TripliceViralAdulto20A29);
        public Color SituacaoTripliceViral2Adulto => SituacaoVacinal(VaccineDoseKeys.TripliceViralAdulto30A59);
        public Color SituacaodTpaAdulto => SituacaoVacinal(VaccineDoseKeys.DtpaAdulto);

        public Color SituacaoHepatiteBIdoso => SituacaoVacinal(VaccineDoseKeys.HepatiteBIdoso);
        public Color SituacaodTIdoso => SituacaoVacinal(VaccineDoseKeys.DtIdoso);
        public Color SituacaoFebreAmarelaIdoso => SituacaoVacinal(VaccineDoseKeys.FebreAmarelaIdoso);
        public Color SituacaodTpaIdoso => SituacaoVacinal(VaccineDoseKeys.DtpaIdoso);

        public Color SituacaoHBGestante => SituacaoVacinal(VaccineDoseKeys.HepatiteBGestante);
        public Color SituacaodTGestante => SituacaoVacinal(VaccineDoseKeys.DtGestante);
        public Color SituacaodTpaGestante => SituacaoVacinal(VaccineDoseKeys.DtpaGestante);

        #endregion

        public VaccinesPageViewModel(IPatientService patientService, IVaccineService vaccineService, IPopupService popupService)
        {
            _patientService = patientService;
            _vaccineService = vaccineService;
            _popupService = popupService;
            OpenVaccineInfo = new Command<string>(async vaccine => await OpenVaccineInfoCommand(vaccine));
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

                var schedule = await _vaccineService.GetScheduleForPatientAsync(patientId);

                if (loadVersion != _loadVersion)
                {
                    return;
                }

                if (schedule is null)
                {
                    await DisplayAlertAsync("Erro", "Cartão vacinal não encontrado.", "Voltar");
                    return;
                }

                HealthRecord = patient;
                Vaccines = new VaccineSchedulePresentation(schedule);
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

        partial void OnVaccinesChanged(VaccineSchedulePresentation? value)
        {
            RefreshVaccineSections(value);
            OnPropertyChanged(string.Empty);
        }

        private void RefreshVaccineSections(VaccineSchedulePresentation? value)
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

        private bool GetVaccineStatus(string vaccine)
        {
            return Vaccines?.GetVaccineStatus(vaccine) ?? false;
        }

        private Color SituacaoVacinal(params string[] doseKeys)
        {
            if (Vaccines is null || doseKeys.Length == 0)
            {
                return ThemeColors.ControlPressed;
            }

            var statuses = doseKeys.Select(GetVaccineStatus).ToArray();
            if (statuses.All(status => status))
            {
                return Colors.Green;
            }

            if (statuses.All(status => !status))
            {
                return Colors.Red;
            }

            return Colors.Orange;
        }

        private async Task OpenVaccineInfoCommand(string vaccine)
        {
            try
            {
                if (Vaccines is null)
                {
                    return;
                }

                var vaccineChecked = GetVaccineStatus(vaccine);
                var popup = new VaccinesInfo(vaccine, vaccineChecked);
                var status = await _popupService.ShowAsync<bool>(popup);

                if (status.WasDismissed)
                {
                    return;
                }

                if (status.Result is bool vaccineStatus && vaccineStatus != vaccineChecked)
                {
                    await UpdateVaccine(vaccine, vaccineStatus);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private async Task UpdateVaccine(string vaccine, bool vaccineStatus)
        {
            try
            {
                if (_loadedPatientId is not int patientId)
                {
                    return;
                }

                await _vaccineService.SetDoseStatusAsync(patientId, vaccine, vaccineStatus);
                var schedule = await _vaccineService.GetScheduleForPatientAsync(patientId);

                if (schedule is not null)
                {
                    Vaccines = new VaccineSchedulePresentation(schedule);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }
    }
}
