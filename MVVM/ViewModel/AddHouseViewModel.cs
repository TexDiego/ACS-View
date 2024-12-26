using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public partial class AddHouseViewModel : BaseViewModel
    {
        private readonly HouseService _houseService;

        public bool IsEdit { get; set; }
        public int HouseId { get; set; }
        public string CEP { get; set; }
        public string Rua { get; set; }
        public string Numero { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Complemento { get; set; }
        public bool PossuiComplemento { get; set; }
        public string Estado { get; set; }
        public string Pais { get; set; } = "Brasil";

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand SalvarCommand { get; }

        public AddHouseViewModel() { }

        public AddHouseViewModel(HouseService houseService) : base()
        {
            _houseService = houseService;
            SalvarCommand = new Command(SalvarCasa);
        }

        private bool IsNullOrWhiteSpaceVerifier()
        {
            List<string> items = new List<string>
            {
                CEP, Rua, Bairro, Cidade, Estado, Pais
            };

            foreach (string item in items)
            {
                if (string.IsNullOrWhiteSpace(item)) return true;
            }

            return false;
        }

        private async void SalvarCasa()
        {
            if (IsNullOrWhiteSpaceVerifier())
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Ops", "Preencha todos os campos obrigatórios.", false, "", true, "OK"));
                return;
            }

            int IdAtual = await _houseService.GetMaxIdAsync();
            Console.WriteLine(IdAtual + "\n\n");

            var novoCadastro = new House
            {
                CasaId = IsEdit ? HouseId : IdAtual + 1,
                CEP = CEP,
                Rua = Rua,
                Bairro = Bairro,
                NumeroCasa = Numero,
                Cidade = Cidade,
                Estado = Estado,
                Pais = Pais,
                Complemento = Complemento,
                PossuiComplemento = PossuiComplemento
            };
            Console.WriteLine("ID: " + novoCadastro.CasaId);
            try
            {
                if (HouseId > 0)
                {
                    // Verifica se o registro já existe no banco
                    var registroExistente = await _houseService.GetHousesById(HouseId);

                    if (registroExistente != null)
                    {
                        // Atualiza o registro existente
                        novoCadastro.CasaId = registroExistente.CasaId;
                        await _houseService.UpdateHouse(novoCadastro);

                        await Application.Current.MainPage.ShowPopupAsync(
                            new DisplayPopUp("Sucesso", "Residência atualizada com sucesso.", false, "", true, "OK"));
                    }
                    else
                    {
                        await Application.Current.MainPage.ShowPopupAsync(
                            new DisplayPopUp("Erro", "ID inválido para atualização.", true, "Voltar", false, ""));
                    }
                }
                else
                {
                    // Criação de um novo registro
                    await _houseService.SaveHouseAsync(novoCadastro);

                    await Application.Current.MainPage.ShowPopupAsync(
                        new DisplayPopUp("Sucesso", "Nova residência criada com sucesso.", false, "", true, "OK"));
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }

            LimparCampos();
        }

        private void LimparCampos()
        {
            CEP = string.Empty;
            HouseId = 0;
            Rua = string.Empty;
            Numero = string.Empty;
            Complemento = string.Empty;
            Cidade = string.Empty;
            Bairro = string.Empty;
            Estado = string.Empty;
            Pais = string.Empty;
        }
    }
}