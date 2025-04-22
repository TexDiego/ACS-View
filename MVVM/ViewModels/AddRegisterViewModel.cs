using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public class AddRegisterViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;
        private readonly VaccineService _vaccineService;

        #region propriedades
        public string Nome { get; set; }
        public string? MotherName { get; set; }
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
        public bool Mental { get; set; }
        public bool Fumante { get; set; }
        public bool Alcoolatra { get; set; }
        public bool Deficiente { get; set; }
        public bool BolsaFamilia { get; set; }
        public bool HeartDisease { get; set; }
        public bool KidneyDisease { get; set; }
        public bool LiverDisease { get; set; }
        public bool LungsDisease { get; set; }
        public bool Neurodivergent { get; set; }
        public bool Addicted { get; set; }
        public bool HIV { get; set; }
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
        #endregion

        // Comando para salvar o cadastro
        public ICommand SalvarCommand { get; }

        public AddRegisterViewModel() { }
        public AddRegisterViewModel(HealthRecordService healthRecordService, VaccineService vaccineService) : base()
        {
            _healthRecordService = healthRecordService;
            _vaccineService = vaccineService;
            SalvarCommand = new Command(async () => await SalvarCadastro());
        }

        private async Task SalvarCadastro()
        {
            if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(NumeroSUS))
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Nome e Número do SUS são obrigatórios.", "OK");
                return;
            }

            var registroExistente = await _healthRecordService.GetRecordBySusAsync(NumeroSUS);

            if (registroExistente != null)
                await AtualizarCadastro(registroExistente);
            else
                await NovoCadastro();

            if (Application.Current.MainPage.BindingContext is OverallViewModel overallViewModel)
            {
                overallViewModel.AtualizarContagens();
            }

            LimparCampos();
        }

        private async Task NovoCadastro()
        {
            try
            {
                var novoCadastro = new HealthRecord
                {
                    Name = Nome,
                    MotherName = MotherName,
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
                    IsNeurodivergent = Neurodivergent,
                    IsDrugAddicted = Addicted,
                    HasHIV = HIV,
                    HasHeartDesease = HeartDisease,
                    HasKidneyDesease = KidneyDisease,
                    HasLiverDesease = LiverDisease,
                    HasLungsDesease = LungsDisease,
                    BolsaFamilia = BolsaFamilia,
                    IsSmoker = Fumante,
                    IsAlcoholic = Alcoolatra,
                    HasCancer = Cancer,
                    Observacao = Observacao,
                    HouseId = HouseId,
                    FamilyId = FamilyId
                };

                var novaVacina = new Vaccines
                {
                    SusNumber = NumeroSUS,
                    BirthDate = Nascimento,
                    BCG_Infantil = false,
                    HepatitisBAoNascer_Infantil = false,
                    Penta1_Infantil = false,
                    VIP1_Infantil = false,
                    Pneumo10_1_Infantil = false,
                    VRH1_Infantil = false,
                    MeningoC1_Infantil = false,
                    Penta2_Infantil = false,
                    VIP2_Infantil = false,
                    Pneumo10_2_Infantil = false,
                    VRH2_Infantil = false,
                    MeningoC2_Infantil = false,
                    Penta3_Infantil = false,
                    VIP3_Infantil = false,
                    Covid1_Infantil = false,
                    Covid2_Infantil = false,
                    FebreAmarela1_Infantil = false,
                    Pneumo10_3_Infantil = false,
                    MeningoC3_Infantil = false,
                    TripliceViral_Infantil = false,
                    DTP1_Infantil = false,
                    VIP4_Infantil = false,
                    HepatiteA_Infantil = false,
                    TetraViral_Infantil = false,
                    DTP2_Infantil = false,
                    FebreAmarela2_Infantil = false,
                    Varicela_Infantil = false,
                    FebreAmarela3_Infantil = false,
                    Pneumo23_Infantil = false,
                    DT_Infantil = false,
                    HPV_Infantil = false
                };

                await _healthRecordService.AdicionarCadastroAsync(novoCadastro);
                await _vaccineService.AdicionarVacinasAsync(novaVacina);
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Sucesso", "Cadastro adicionado com sucesso.", false, "", true, "OK"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async Task AtualizarCadastro(HealthRecord registroExistente)
        {
            try
            {
                var novoCadastro = new HealthRecord
                {
                    Name = Nome,
                    MotherName = MotherName,
                    SusNumber = registroExistente.SusNumber,
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
                    IsNeurodivergent = Neurodivergent,
                    IsDrugAddicted = Addicted,
                    HasHIV = HIV,
                    HasHeartDesease = HeartDisease,
                    HasKidneyDesease = KidneyDisease,
                    HasLiverDesease = LiverDisease,
                    HasLungsDesease = LungsDisease,
                    BolsaFamilia = BolsaFamilia,
                    IsSmoker = Fumante,
                    IsAlcoholic = Alcoolatra,
                    HasCancer = Cancer,
                    Observacao = Observacao,
                    HouseId = HouseId,
                    FamilyId = FamilyId
                };

                await _healthRecordService.AtualizarCadastroAsync(novoCadastro);
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Sucesso", "Cadastro atualizado com sucesso.", false, "", true, "Voltar"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private void LimparCampos()
        {
            Nome = string.Empty;
            MotherName = string.Empty;
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
            Alcoolatra = false;
            BolsaFamilia = false;
            HeartDisease = false;
            KidneyDisease = false;
            LiverDisease = false;
            LungsDisease = false;
            Neurodivergent = false;
            Addicted = false;
            HIV = false;
            Deficiente = false;
            Cancer = false;
            Observacao = string.Empty;
            HasObs = false;
            FamilyId = 0;
            HouseId = 0;

            OnPropertyChanged(nameof(Nome));
            OnPropertyChanged(nameof(MotherName));
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
            OnPropertyChanged(nameof(BolsaFamilia));
            OnPropertyChanged(nameof(HeartDisease));
            OnPropertyChanged(nameof(KidneyDisease));
            OnPropertyChanged(nameof(LiverDisease));
            OnPropertyChanged(nameof(LungsDisease));
            OnPropertyChanged(nameof(Neurodivergent));
            OnPropertyChanged(nameof(Addicted));
            OnPropertyChanged(nameof(HIV));
            OnPropertyChanged(nameof(Alcoolatra));
            OnPropertyChanged(nameof(Observacao));
            OnPropertyChanged(nameof(Deficiente));
            OnPropertyChanged(nameof(HasObs));
            OnPropertyChanged(nameof(Cancer));
            OnPropertyChanged(nameof(HouseId));
            OnPropertyChanged(nameof(FamilyId));
        }
    }
}