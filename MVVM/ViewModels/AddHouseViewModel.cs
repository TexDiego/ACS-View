using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public partial class AddHouseViewModel : BaseViewModel
    {
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();
        
        [ObservableProperty] private House houseModel = new();
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isRunning;

        public ICommand SalvarCommand => new Command(async () => await SalvarCasa());
        public ICommand SearchCEP => new Command(async () => await SearchCEPAsync());
        public ICommand GoBack => new Command(async () => await Shell.Current.Navigation.PopAsync());

        private bool IsNullOrWhiteSpaceVerifier()
        {
            // Verifica se alguma propriedade obrigatória está nula ou em branco
            return string.IsNullOrWhiteSpace(HouseModel.CEP)
                || string.IsNullOrWhiteSpace(HouseModel.Rua)
                || string.IsNullOrWhiteSpace(HouseModel.Bairro)
                || string.IsNullOrWhiteSpace(HouseModel.Cidade)
                || string.IsNullOrWhiteSpace(HouseModel.Estado)
                || string.IsNullOrWhiteSpace(HouseModel.Pais);
        }

        private async Task SalvarCasa()
        {
            if (IsNullOrWhiteSpaceVerifier())
            {
                await Shell.Current.ShowPopupAsync(
                    new DisplayPopUp("Ops", "Preencha todos os campos obrigatórios.", false, "", true, "OK"));
                return;
            }

            try
            {
                if (HouseModel.CasaId > 0)
                {
                    await _houseService.UpdateHouseAsync(HouseModel);

                    await Shell.Current.ShowPopupAsync(
                        new DisplayPopUp("Sucesso", "Residência atualizada com sucesso.", false, "", true, "OK"));
                }
                else
                {
                    await _houseService.SaveHouseAsync(HouseModel);

                    await Shell.Current.ShowPopupAsync(
                        new DisplayPopUp("Sucesso", "Nova residência criada com sucesso.", false, "", true, "OK"));
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.ShowPopupAsync(
                    new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }

            HouseModel = new();
        }

        private async Task SearchCEPAsync()
        {
            if (!ValidarCep(HouseModel.CEP))
            {
                await Shell.Current.ShowPopupAsync(new DisplayPopUp("Ops", "CEP inválido", false, "", true, "Voltar"));
                return;
            }

            try
            {
                IsRunning = true;

                var endereco = await CEPService.BuscarEnderecoPorCep(HouseModel.CEP);

                if (endereco != null && !string.IsNullOrEmpty(endereco.Rua))
                {
                    endereco.CEP = endereco.CEP.Replace("-", "");

                    HouseModel = endereco;

                    IsRunning = false;
                }
                else
                {
                    IsRunning = false;
                    await Shell.Current.ShowPopupAsync(new DisplayPopUp("Erro", "Endereço não encontrado ou CEP inválido.", false, "", true, "OK"));
                }
            }
            catch (HttpRequestException ex)
            {
                await Shell.Current.ShowPopupAsync(new DisplayPopUp("Erro", $"Não foi possível conectar ao serviço de CEP.\nVerifique sua conexão com a internet.\n\n {ex.Message}", false, "", true, "OK"));
            }
            catch (Exception ex)
            {
                await Shell.Current.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private bool ValidarCep(string cep)
        {
            return !string.IsNullOrWhiteSpace(cep) && cep.Length == 8 && cep.All(char.IsDigit);
        }
    }
}