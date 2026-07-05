using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class AddHouseViewModel(IHouseService _houseService, ICepService _cepService) : BaseViewModel
    {
        [ObservableProperty] private House houseModel = new();

        public ICommand SalvarCommand => new Command(async () => await SalvarCasa());
        public ICommand SearchCEP => new Command(async () => await SearchCEPAsync());
        public ICommand GoBack => new Command(async () => await NavigateBackAsync());

        public async Task LoadHouseAsync(int houseId)
        {
            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    HouseModel = await _houseService.GetHouseByIdAsync(houseId) ?? new House();
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

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
                await DisplayAlertAsync("Ops", "Preencha todos os campos obrigatórios.");
                return;
            }

            try
            {
                if (HouseModel.CasaId > 0)
                {
                    await _houseService.UpdateHouseAsync(HouseModel);

                    await DisplayAlertAsync("Sucesso", "Residência atualizada com sucesso.");
                }
                else
                {
                    await _houseService.SaveHouseAsync(HouseModel);

                    await DisplayAlertAsync("Sucesso", "Nova residência criada com sucesso.");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }

            HouseModel = new();
        }

        private async Task SearchCEPAsync()
        {
            if (!ValidarCep(HouseModel.CEP))
            {
                await DisplayAlertAsync("Ops", "CEP inválido", "Voltar");
                return;
            }

            try
            {
                await ExecuteWithRunningAsync(async () =>
                {
                    var endereco = await _cepService.GetAddressByCepAsync(HouseModel.CEP);

                    if (endereco != null && !string.IsNullOrEmpty(endereco.Rua))
                    {
                        HouseModel = MergeCepAddress(HouseModel, endereco);
                        return;
                    }

                    await DisplayAlertAsync("Erro", "Endereço não encontrado ou CEP inválido.");
                });
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlertAsync("Erro", $"Não foi possível conectar ao serviço de CEP.\nVerifique sua conexão com a internet.\n\n {ex.Message}");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private static bool ValidarCep(string cep)
        {
            return CepNumberRules.IsValid(cep);
        }

        private static House MergeCepAddress(House current, House address)
        {
            return new House
            {
                CasaId = current.CasaId,
                UserId = current.UserId,
                CEP = CepNumberRules.Normalize(address.CEP),
                Rua = CoalesceAddressValue(address.Rua, current.Rua),
                TipoLogradouro = CoalesceAddressValue(address.TipoLogradouro, current.TipoLogradouro),
                NumeroCasa = current.NumeroCasa,
                SearchRua = current.SearchRua,
                SearchComplemento = current.SearchComplemento,
                Bairro = CoalesceAddressValue(address.Bairro, current.Bairro),
                Cidade = IsSuspiciousCity(address.Cidade)
                    ? current.Cidade
                    : CoalesceAddressValue(address.Cidade, current.Cidade),
                Estado = CoalesceAddressValue(address.Estado, current.Estado),
                Pais = CoalesceAddressValue(address.Pais, current.Pais),
                Complemento = current.Complemento,
                PossuiComplemento = current.PossuiComplemento || !string.IsNullOrWhiteSpace(current.Complemento)
            };
        }

        private static string CoalesceAddressValue(string? preferred, string? fallback)
        {
            return string.IsNullOrWhiteSpace(preferred)
                ? fallback?.Trim() ?? string.Empty
                : preferred.Trim();
        }

        private static bool IsSuspiciousCity(string? value)
        {
            var normalized = value?.Trim();
            return !string.IsNullOrWhiteSpace(normalized) && normalized.All(char.IsDigit);
        }
    }
}
