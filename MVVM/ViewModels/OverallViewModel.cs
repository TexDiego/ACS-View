using ACS_View.MVVM.Models.Services;

namespace ACS_View.MVVM.ViewModels
{
    public partial class OverallViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;
        private readonly HouseService _houseService;

        #region Properties
        private int _totalGestantes;
        public int TotalGestantes
        {
            get => _totalGestantes;
            set
            {
                _totalGestantes = value;
                OnPropertyChanged();
            }
        }

        private int _totalDiabeticos;
        public int TotalDiabeticos
        {
            get => _totalDiabeticos;
            set
            {
                _totalDiabeticos = value;
                OnPropertyChanged();
            }
        }

        private int _totalHipertensos;
        public int TotalHipertensos
        {
            get => _totalHipertensos;
            set
            {
                _totalHipertensos = value;
                OnPropertyChanged();
            }
        }

        private int _totalDiabetesHipertensao;
        public int TotalDiabetesHipertensao
        {
            get => _totalDiabetesHipertensao;
            set
            {
                _totalDiabetesHipertensao = value;
                OnPropertyChanged();
            }
        }

        private int _totalTuberculose;
        public int TotalTuberculose
        {
            get => _totalTuberculose;
            set
            {
                _totalTuberculose = value;
                OnPropertyChanged();
            }
        }

        private int _totalHanseniase;
        public int TotalHanseniase
        {
            get => _totalHanseniase;
            set
            {
                _totalHanseniase = value;
                OnPropertyChanged();
            }
        }

        private int _totalAcamados;
        public int TotalAcamados
        {
            get => _totalAcamados;
            set
            {
                _totalAcamados = value;
                OnPropertyChanged();
            }
        }

        private int _totalDomiciliados;
        public int TotalDomiciliados
        {
            get => _totalDomiciliados;
            set
            {
                _totalDomiciliados = value;
                OnPropertyChanged();
            }
        }

        private int _totalMenores6Anos;
        public int TotalMenores6Anos
        {
            get => _totalMenores6Anos;
            set
            {
                _totalMenores6Anos = value;
                OnPropertyChanged();
            }
        }

        private int _TotalMental;
        public int TotalMental
        {
            get => _TotalMental;
            set
            {
                _TotalMental = value;
                OnPropertyChanged();
            }
        }

        private int _TotalFumante;
        public int TotalFumante
        {
            get => _TotalFumante;
            set
            {
                _TotalFumante = value;
                OnPropertyChanged();
            }
        }

        private int _TotalAlcoolatra;
        public int TotalAlcoolatra
        {
            get => _TotalAlcoolatra;
            set
            {
                _TotalAlcoolatra = value;
                OnPropertyChanged();
            }
        }

        private int _TotalDeficiente;
        public int TotalDeficiente
        {
            get => _TotalDeficiente;
            set
            {
                _TotalDeficiente = value;
                OnPropertyChanged();
            }
        }

        private int _TotalHeartDesease;
        public int TotalHeartDesease
        {
            get => _TotalHeartDesease;
            set
            {
                _TotalHeartDesease = value;
                OnPropertyChanged();
            }
        }

        private int _TotalKidneyDesease;
        public int TotalKidneyDesease
        {
            get => _TotalKidneyDesease;
            set
            {
                _TotalKidneyDesease = value;
                OnPropertyChanged();
            }
        }

        private int _TotalLungDesease;
        public int TotalLungDesease
        {
            get => _TotalLungDesease;
            set
            {
                _TotalLungDesease = value;
                OnPropertyChanged();
            }
        }

        private int _TotalLiverDesease;
        public int TotalLiverDesease
        {
            get => _TotalLiverDesease;
            set
            {
                _TotalLiverDesease = value;
                OnPropertyChanged();
            }
        }

        private int _TotalBolsaFamilia;
        public int TotalBolsaFamilia
        {
            get => _TotalBolsaFamilia;
            set
            {
                _TotalBolsaFamilia = value;
                OnPropertyChanged();
            }
        }

        private int _TotalNeurodivergents;
        public int TotalNeurodivergents
        {
            get => _TotalNeurodivergents;
            set
            {
                _TotalNeurodivergents = value;
                OnPropertyChanged();
            }
        }

        private int _TotalDrugsAddicted;
        public int TotalDrugsAddicted
        {
            get => _TotalDrugsAddicted;
            set
            {
                _TotalDrugsAddicted = value;
                OnPropertyChanged();
            }
        }

        private int _TotalHIV;
        public int TotalHIV
        {
            get => _TotalHIV;
            set
            {
                _TotalHIV = value;
                OnPropertyChanged();
            }
        }

        private int _TotalCancer;
        public int TotalCancer
        {
            get => _TotalCancer;
            set
            {
                _TotalCancer = value;
                OnPropertyChanged();
            }
        }

        private int _TotalOld;
        public int TotalOld
        {
            get => _TotalOld;
            set
            {
                _TotalOld = value;
                OnPropertyChanged();
            }
        }

        private int _totalHouses;
        public int TotalHouses
        {
            get => _totalHouses;
            set
            {
                _totalHouses = value;
                OnPropertyChanged();
            }
        }

        private int _Total;
        public int Total
        {
            get => _Total;
            set
            {
                _Total = value;
                OnPropertyChanged();
            }
        }

        private int _NoResidence;
        public int NoResidence
        {
            get => _NoResidence;
            set
            {
                _NoResidence = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        #endregion

        public OverallViewModel() { }

        public OverallViewModel(HealthRecordService healthRecordService, HouseService houseService)
        {
            _healthRecordService = healthRecordService;
            _houseService = houseService;
            AtualizarContagens();
        }

        public async void AtualizarContagens()
        {
            try
            {
                IsLoading = true;

                TotalHouses = await _houseService.GetTotalCountAsync();
                TotalGestantes = await _healthRecordService.GetConditionCountAsync(r => r.IsPregnant);
                TotalDiabeticos = await _healthRecordService.GetConditionCountAsync(r => r.HasDiabetes);
                TotalHipertensos = await _healthRecordService.GetConditionCountAsync(r => r.HasHypertension);
                TotalDiabetesHipertensao = await _healthRecordService.GetConditionCountAsync(r => r.HasDiabetes && r.HasHypertension);
                TotalTuberculose = await _healthRecordService.GetConditionCountAsync(r => r.HasTuberculosis);
                TotalHanseniase = await _healthRecordService.GetConditionCountAsync(r => r.HasLeprosy);
                TotalAcamados = await _healthRecordService.GetConditionCountAsync(r => r.IsBedridden);
                TotalDomiciliados = await _healthRecordService.GetConditionCountAsync(r => r.IsHomebound);
                TotalMenores6Anos = await _healthRecordService.GetYoungers();
                TotalMental = await _healthRecordService.GetConditionCountAsync(r => r.HasMentalIllness);
                TotalDeficiente = await _healthRecordService.GetConditionCountAsync(r => r.HasDisabilities);
                TotalFumante = await _healthRecordService.GetConditionCountAsync(r => r.IsSmoker);
                TotalAlcoolatra = await _healthRecordService.GetConditionCountAsync(r => r.IsAlcoholic);
                TotalBolsaFamilia = await _healthRecordService.GetConditionCountAsync(r => r.BolsaFamilia);
                TotalHeartDesease = await _healthRecordService.GetConditionCountAsync(r => r.HasHeartDesease);
                TotalKidneyDesease = await _healthRecordService.GetConditionCountAsync(r => r.HasKidneyDesease);
                TotalLungDesease = await _healthRecordService.GetConditionCountAsync(r => r.HasLungsDesease);
                TotalLiverDesease = await _healthRecordService.GetConditionCountAsync(r => r.HasLiverDesease);
                TotalNeurodivergents = await _healthRecordService.GetConditionCountAsync(r => r.IsNeurodivergent);
                TotalDrugsAddicted = await _healthRecordService.GetConditionCountAsync(r => r.IsDrugAddicted);
                TotalHIV = await _healthRecordService.GetConditionCountAsync(r => r.HasHIV);
                TotalCancer = await _healthRecordService.GetConditionCountAsync(r => r.HasCancer);
                TotalOld = await _healthRecordService.GetElder();
                Total = await _healthRecordService.GetTotalCountAsync();
                NoResidence = await _healthRecordService.GetConditionCountAsync(r => r.HouseId == 0);

            }
            catch
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível carregar os dados", "Voltar");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

}