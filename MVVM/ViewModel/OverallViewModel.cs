using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using System.Collections.ObjectModel;

namespace ACS_View.MVVM.ViewModel
{
    public class OverallViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;

        private ObservableCollection<HealthRecord> _pessoasCom60AnosOuMais;
        public ObservableCollection<HealthRecord> PessoasCom60AnosOuMais
        {
            get => _pessoasCom60AnosOuMais;
            set
            {
                _pessoasCom60AnosOuMais = value;
                OnPropertyChanged();
            }
        }

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

        private int _totalMenores2Anos;
        public int TotalMenores2Anos
        {
            get => _totalMenores2Anos;
            set
            {
                _totalMenores2Anos = value;
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

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public OverallViewModel() { }

        public OverallViewModel(HealthRecordService healthRecordService)
        {
            _healthRecordService = healthRecordService;
            AtualizarContagens();
        }

        public async void AtualizarContagens()
        {
            try
            {
                IsLoading = true;

                TotalGestantes = await _healthRecordService.GetConditionCountAsync(r => r.IsPregnant);
                TotalDiabeticos = await _healthRecordService.GetConditionCountAsync(r => r.HasDiabetes);
                TotalHipertensos = await _healthRecordService.GetConditionCountAsync(r => r.HasHypertension);
                TotalDiabetesHipertensao = await _healthRecordService.GetConditionCountAsync(r => r.HasDiabetes && r.HasHypertension);
                TotalTuberculose = await _healthRecordService.GetConditionCountAsync(r => r.HasTuberculosis);
                TotalHanseniase = await _healthRecordService.GetConditionCountAsync(r => r.HasLeprosy);
                TotalAcamados = await _healthRecordService.GetConditionCountAsync(r => r.IsBedridden);
                TotalDomiciliados = await _healthRecordService.GetConditionCountAsync(r => r.IsHomebound);
                TotalMenores2Anos = await _healthRecordService.GetYoungers();
                TotalMental = await _healthRecordService.GetConditionCountAsync(r => r.HasMentalIllness);
                TotalDeficiente = await _healthRecordService.GetConditionCountAsync(r => r.HasDisabilities);
                TotalFumante = await _healthRecordService.GetConditionCountAsync(r => r.IsSmoker);
                TotalCancer = await _healthRecordService.GetConditionCountAsync(r => r.HasCancer);
                TotalOld = await _healthRecordService.GetElder();
                Total = await _healthRecordService.GetTotalCountAsync();
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
