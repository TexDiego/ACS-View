using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public class AddRegisterViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;

        public string Nome { get; set; }
        public string NumeroSUS { get; set; }
        public DateTime Nascimento { get; set; }
        public int Idade { get; set; }
        public bool Gestante { get; set; }
        public bool Diabetes { get; set; }
        public bool Hipertensao { get; set; }
        public bool Hanseniase { get; set; }
        public bool Tuberculose { get; set; }
        public bool Acamado { get; set; }
        public bool Domiciliado { get; set; }
        public bool MenorDe2Anos { get; set; }
        public bool Mental { get; set; }
        public bool Fumante { get; set; }
        public bool Deficiente { get; set; }
        public bool Cancer { get; set; }
        public string? Observacao { get; set; }
        public bool HasObs { get; set; }
        public int FamilyId { get; set; }
        public int HouseId { get; set; }

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Comando para salvar o cadastro
        public ICommand SalvarCommand { get; }

        public AddRegisterViewModel() { }
        public AddRegisterViewModel(HealthRecordService healthRecordService) : base()
        {
            _healthRecordService = healthRecordService;
            SalvarCommand = new Command(SalvarCadastro);
        }

        private async void SalvarCadastro()
        {
            if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(NumeroSUS))
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Nome e Número do SUS são obrigatórios.", "OK");
                return;
            }

            var novoCadastro = new HealthRecord
            {
                Name = Nome,
                SusNumber = NumeroSUS,
                BirthDate = Nascimento,
                IsPregnant = Gestante,
                HasDiabetes = Diabetes,
                HasHypertension = Hipertensao,
                HasLeprosy = Hanseniase,
                HasTuberculosis = Tuberculose,
                IsHomebound = Acamado,
                IsBedridden = Domiciliado,
                HasMentalIllness = Mental,
                HasDisabilities = Deficiente,
                IsSmoker = Fumante,
                HasCancer = Cancer,
                Observacao = Observacao,
                HasObs = HasObs,
                HouseId = HouseId,
                FamilyId = FamilyId
            };

            try
            {
                var registroExistente = await _healthRecordService.GetRecordBySusAsync(NumeroSUS);

                if (registroExistente != null)
                {
                    // Atualiza o cadastro existente
                    novoCadastro.SusNumber = registroExistente.SusNumber; // Certifique-se de atualizar o mesmo ID.
                    await _healthRecordService.AtualizarCadastroAsync(novoCadastro);
                    await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Sucesso", "Cadastro atualizado com sucesso.", false, "", true, "Voltar"));
                }
                else
                {
                    // Adiciona novo cadastro
                    await _healthRecordService.AdicionarCadastroAsync(novoCadastro);
                    await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Sucesso", "Cadastro adicionado com sucesso.", false, "", true, "OK"));
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }

            if (Application.Current.MainPage.BindingContext is OverallViewModel overallViewModel)
            {
                overallViewModel.AtualizarContagens();
            }

            // Limpa os campos após o salvamento
            LimparCampos();
        }

        private void LimparCampos()
        {
            Nome = string.Empty;
            NumeroSUS = string.Empty;
            Nascimento = DateTime.Today;
            Idade = 0;
            Gestante = false;
            Diabetes = false;
            Hipertensao = false;
            Acamado = false;
            Domiciliado = false;
            Hanseniase = false;
            Tuberculose = false;
            Mental = false;
            Fumante = false;
            Deficiente = false;
            Cancer = false;
            Observacao = string.Empty;
            HasObs = false;
            FamilyId = 0;
            HouseId = 0;

            OnPropertyChanged(nameof(Nome));
            OnPropertyChanged(nameof(NumeroSUS));
            OnPropertyChanged(nameof(Nascimento));
            OnPropertyChanged(nameof(Idade));
            OnPropertyChanged(nameof(Gestante));
            OnPropertyChanged(nameof(Diabetes));
            OnPropertyChanged(nameof(Hipertensao));
            OnPropertyChanged(nameof(Acamado));
            OnPropertyChanged(nameof(Domiciliado));
            OnPropertyChanged(nameof(Hanseniase));
            OnPropertyChanged(nameof(Tuberculose));
            OnPropertyChanged(nameof(Mental));
            OnPropertyChanged(nameof(Fumante));
            OnPropertyChanged(nameof(Observacao));
            OnPropertyChanged(nameof(Deficiente));
            OnPropertyChanged(nameof(HasObs));
            OnPropertyChanged(nameof(Cancer));
            OnPropertyChanged(nameof(HouseId));
            OnPropertyChanged(nameof(FamilyId));
        }
    }
}