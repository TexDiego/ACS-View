using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public class CadastroViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;

        public string Name { get; set; }
        public string SusNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsPregnant { get; set; }
        public bool HasDiabetes { get; set; }
        public bool HasHypertension { get; set; }
        public bool HasTuberculosis { get; set; }
        public bool HasLeprosy { get; set; }
        public bool IsBedridden { get; set; }
        public bool IsHomebound { get; set; }
        public bool IsUnderTwoYears { get; set; }
        public bool HasMentalIllness { get; set; }
        public bool IsSmoker { get; set; }
        public bool IsDisabled { get; set; }
        public bool HasCancer { get; set; }
        public bool IsOld { get; set; }

        public ICommand SaveCommand { get; }

        public CadastroViewModel() { }

        public CadastroViewModel(HealthRecordService healthRecordService)
        {
            _healthRecordService = healthRecordService;
            SaveCommand = new Command(async () => await SaveRecordAsync());
        }

        private async Task SaveRecordAsync()
        {
            if (string.IsNullOrWhiteSpace(SusNumber))
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "O número do SUS é obrigatório.", "OK");
                return;
            }

            var existingRecord = await _healthRecordService.GetRecordBySusAsync(SusNumber);

            var record = new HealthRecord
            {
                SusNumber = SusNumber,
                Name = Name,
                BirthDate = BirthDate,
                IsPregnant = IsPregnant,
                HasDiabetes = HasDiabetes,
                HasHypertension = HasHypertension,
                HasTuberculosis = HasTuberculosis,
                HasLeprosy = HasLeprosy,
                IsBedridden = IsBedridden,
                IsHomebound = IsHomebound,
                HasMentalIllness = HasMentalIllness,
                IsSmoker = IsSmoker,
                HasDisabilities = IsDisabled,
                HasCancer = HasCancer
            };

            if (existingRecord != null)
            {
                await _healthRecordService.SaveRecordAsync(record);
                await Application.Current.MainPage.DisplayAlert("Atualizado", "Cadastro atualizado com sucesso!", "OK");
            }
            else
            {
                await _healthRecordService.SaveRecordAsync(record);
                await Application.Current.MainPage.DisplayAlert("Cadastrado", "Cadastro adicionado com sucesso!", "OK");
            }

            ClearFields();
        }

        private void ClearFields()
        {
            Name = string.Empty;
            SusNumber = string.Empty;
            BirthDate = DateTime.Now;
            IsPregnant = false;
            HasDiabetes = false;
            HasHypertension = false;
            HasTuberculosis = false;
            HasLeprosy = false;
            IsBedridden = false;
            IsHomebound = false;
            IsUnderTwoYears = false;
            HasMentalIllness = false;
            IsDisabled = false;
            IsSmoker = false;
            HasCancer = false;
            IsOld = false;

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(SusNumber));
            OnPropertyChanged(nameof(BirthDate));
            OnPropertyChanged(nameof(IsPregnant));
            OnPropertyChanged(nameof(HasDiabetes));
            OnPropertyChanged(nameof(HasHypertension));
            OnPropertyChanged(nameof(HasTuberculosis));
            OnPropertyChanged(nameof(HasLeprosy));
            OnPropertyChanged(nameof(IsBedridden));
            OnPropertyChanged(nameof(IsHomebound));
            OnPropertyChanged(nameof(IsUnderTwoYears));
            OnPropertyChanged(nameof(HasMentalIllness));
            OnPropertyChanged(nameof(IsDisabled));
            OnPropertyChanged(nameof(IsSmoker));
            OnPropertyChanged(nameof(HasCancer));
            OnPropertyChanged(nameof(IsOld));
        }
    }
}